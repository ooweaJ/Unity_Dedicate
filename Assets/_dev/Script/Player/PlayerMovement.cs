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

    [Tooltip("네트워크 없이 로컬 단독 테스트 시 true")]
    public bool localMode = false;

    private Rigidbody                 rb;
    private PlayerAnimationController anim;
    private CharacterWeapon           weapon;

    // 팀 1(북쪽)은 X·Z 입력 반전 — 카메라 방향과 일치
    private float _inputSign        = 1f;
    private bool  _inputInitialized = false;

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

    private bool IsControllable => localMode || isLocalPlayer;

    private void Awake()
    {
        rb     = GetComponent<Rigidbody>();
        anim   = GetComponent<PlayerAnimationController>();
        weapon = GetComponent<CharacterWeapon>();

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
        Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
        rb.linearVelocity = new Vector3(flat.x * force, rb.linearVelocity.y, flat.z * force);
        _isKnockedBack    = true;
        _knockbackEndTime = Time.time + KnockbackMoveLockDuration;
    }

    // ─── 이동 ─────────────────────────────────────────────────────────────
    private void Move()
    {
        if (isDashing || _isStunned) return;

        if (_isKnockedBack) return;

        // 공격 액션 중 이동·회전 잠금 — 회전 스냅백 방지 + 뒷걸음 애니메이션 불필요
        if (weapon != null && weapon.IsActing)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 dir = GetMoveDir();

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

    /// 팀 ID 기반 입력 보정 — 팀 1(북쪽)은 X·Z 모두 반전
    /// BattleNetworkPlayer.Local이 확정되는 첫 프레임에 1회 초기화
    private Vector3 GetMoveDir()
    {
        if (!_inputInitialized && BattleNetworkPlayer.Local != null)
        {
            _inputSign        = BattleNetworkPlayer.Local.teamId == 1 ? -1f : 1f;
            _inputInitialized = true;
        }
        return new Vector3(moveInput.x * _inputSign, 0f, moveInput.y * _inputSign);
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
        bool touchedWall = false;

        foreach (ContactPoint c in col.contacts)
        {
            if (Vector3.Angle(c.normal, Vector3.up) <= MaxGroundAngle)
            {
                if (!isJumping) isGrounded = true;
            }
            else
            {
                touchedWall = true;
            }
        }

        // 이미 벽에 붙어있는 상태에서 새 넉백이 들어오면 즉시 벽꿍 처리
        if (touchedWall && _isKnockedBack && isLocalPlayer)
        {
            _isKnockedBack = false;
            CmdNotifyWallHit();
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
        Vector3 dir = GetMoveDir();
        if (dir.magnitude < 0.1f) return 0f;
        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        return Mathf.Clamp(angle / 180f, -1f, 1f);
    }
}
