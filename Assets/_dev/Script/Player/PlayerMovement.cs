using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// 물리 이동 전담 컴포넌트 — 이동/점프/대시/상태이상 반영
/// 상태이상(스턴·슬로우·넉백) 상태에서의 이동 차단은 이 컴포넌트가 담당
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("이동")]
    public float moveSpeed   = 5f;
    public float rotateSpeed = 720f;

    [Header("점프")]
    public float jumpForce = 6f;

    private Rigidbody                 rb;
    private PlayerAnimationController anim;
    private PlayerInputHandler        input;

    private Vector2 moveInput;
    private bool    isDashing     = false;
    private bool    isGrounded    = false;
    private bool    isJumping     = false;
    private int     groundContact = 0;

    // 넉백 상태 — RpcApplyKnockback에서 설정, Move()에서 체크
    private bool  _isKnockedBack    = false;
    private float _knockbackEndTime = 0f;
    private const float KnockbackMoveLockDuration = 0.3f;

    // 스턴·슬로우 — StatusEffectHandler SyncVar 훅에서 설정
    private bool  _isStunned      = false;
    private float _slowMultiplier = 1f;

    private const float MaxGroundAngle = 45f;

    private bool IsControllable => isLocalPlayer || (input != null && input.localMode);

    private void Awake()
    {
        rb    = GetComponent<Rigidbody>();
        anim  = GetComponent<PlayerAnimationController>();
        input = GetComponent<PlayerInputHandler>();

        rb.freezeRotation = true;
        rb.interpolation  = RigidbodyInterpolation.Interpolate;
    }

    // ─── PlayerController가 연결하는 핸들러 ──────────────────────────────
    public void HandleMove(Vector2 v) => moveInput = v;

    public void HandleJump()
    {
        if (!IsControllable || _isStunned) return;
        if (!isGrounded || isJumping) return;

        isJumping  = true;
        isGrounded = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

        anim?.UpdateMovementParams(
            moveInput.magnitude, CalculateDirection(),
            jumpForce, jumpForce, jump: true, rest: false);
    }

    private void FixedUpdate()
    {
        // 넉백/스턴 상태 업데이트 (서버와 로컬 플레이어 모두 수행)
        if (isServer || isLocalPlayer)
        {
            UpdateStatusState();
        }

        if (!IsControllable) return;
        Move();
        SyncAnimation();
    }

    private void UpdateStatusState()
    {
        if (_isKnockedBack)
        {
            if (Time.time >= _knockbackEndTime)
            {
                _isKnockedBack = false;
            }
        }
    }

    // ─── 상태이상 세터 (StatusEffectHandler SyncVar 훅에서 호출) ─────────
    public void SetStunned(bool stunned)
    {
        _isStunned = stunned;
        if (stunned)
        {
            _isKnockedBack    = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    public void SetSlowMultiplier(float multiplier)
    {
        _slowMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
    }

    // ─── 넉백 ─────────────────────────────────────────────────────────────
    [Server]
    public void ServerApplyKnockback(Vector3 dir, float force)
    {
        ApplyKnockbackPhysics(dir, force);
        RpcApplyKnockback(dir, force);
    }

    [ClientRpc]
    private void RpcApplyKnockback(Vector3 dir, float force)
    {
        if (!isLocalPlayer) return;
        ApplyKnockbackPhysics(dir, force);
    }

    private void ApplyKnockbackPhysics(Vector3 dir, float force)
    {
        Debug.Log($"[PlayerMovement] ApplyKnockback: {gameObject.name} IsServer={isServer} IsLocal={isLocalPlayer} Force={force}");
        Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
        rb.linearVelocity = new Vector3(flat.x * force, rb.linearVelocity.y, flat.z * force);
        _isKnockedBack    = true;
        _knockbackEndTime = Time.time + KnockbackMoveLockDuration;
    }

    // ─── 이동 ─────────────────────────────────────────────────────────────
    private void Move()
    {
        if (isDashing || _isStunned) return;

        if (_isKnockedBack)
        {
            // UpdateStatusState에서 처리하므로 여기서는 리턴만 함
            return;
        }

        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);

        if (dir.magnitude < 0.1f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        float speed = moveSpeed * _slowMultiplier;
        rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.fixedDeltaTime);
    }

    // ─── 대시 ─────────────────────────────────────────────────────────────
    public void StartDash(Vector3 dir, float speed, float duration)
    {
        StartCoroutine(DashCoroutine(dir, speed, duration));
    }

    private IEnumerator DashCoroutine(Vector3 dir, float speed, float duration)
    {
        isDashing          = true;
        transform.rotation = Quaternion.LookRotation(dir);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
    }

    // ─── 충돌 판정 ────────────────────────────────────────────────────────
    private void OnCollisionEnter(Collision col)
    {
        bool touchedGround = false;
        bool touchedWall   = false;

        foreach (ContactPoint c in col.contacts)
        {
            if (Vector3.Angle(c.normal, Vector3.up) <= MaxGroundAngle)
                touchedGround = true;
            else
                touchedWall = true;
        }

        if (touchedGround)
        {
            groundContact++;
            if (rb.linearVelocity.y <= 0.1f) isJumping = false;
        }

        // 넉백 중 벽 충돌 → 서버에 기절 요청
        if (touchedWall && _isKnockedBack && isLocalPlayer)
        {
            _isKnockedBack = false;
            CmdNotifyWallHit();
        }
    }

    private void OnCollisionStay(Collision col)
    {
        if (isJumping) return;
        foreach (ContactPoint c in col.contacts)
        {
            if (Vector3.Angle(c.normal, Vector3.up) <= MaxGroundAngle)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision col)
    {
        groundContact = Mathf.Max(0, groundContact - 1);
        if (groundContact == 0) isGrounded = false;
    }

    [Command]
    private void CmdNotifyWallHit()
    {
        Debug.Log("[PlayerMovement] CmdNotifyWallHit: Reporting knockback wall hit to server");
        GetComponent<StatusEffectHandler>()?.OnKnockbackWallHit();
    }

    // ─── 애니메이션 동기화 ────────────────────────────────────────────────
    private void SyncAnimation()
    {
        if (anim == null) return;

        float speed    = _isStunned ? 0f : moveInput.magnitude;
        float dir      = CalculateDirection();
        float jumpH    = isGrounded ? 0f : Mathf.Max(0f, rb.linearVelocity.y);
        float gravCtrl = isGrounded ? 0f : rb.linearVelocity.y;

        anim.UpdateMovementParams(speed, dir, jumpH, gravCtrl, !isGrounded, speed < 0.05f && isGrounded);
    }

    private float CalculateDirection()
    {
        if (moveInput.magnitude < 0.1f) return 0f;
        Vector3 dir   = new Vector3(moveInput.x, 0f, moveInput.y);
        float   angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        return Mathf.Clamp(angle / 180f, -1f, 1f);
    }
}
