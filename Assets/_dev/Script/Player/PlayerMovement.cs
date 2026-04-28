using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// 물리 이동 전담 컴포넌트 — 이동/점프/대시
/// 이벤트 구독은 PlayerController(Mediator)가 담당
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
        if (!IsControllable) return;
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
        if (!IsControllable) return;
        Move();
        SyncAnimation();
    }

    private void Move()
    {
        if (isDashing) return;

        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);

        if (dir.magnitude < 0.1f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.fixedDeltaTime);
    }

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

    private void SyncAnimation()
    {
        if (anim == null) return;

        float speed    = moveInput.magnitude;
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

    // ─── 지면 판정 ───────────────────────────────────────────────────────
    private void OnCollisionEnter(Collision col)
    {
        foreach (ContactPoint c in col.contacts)
        {
            if (Vector3.Angle(c.normal, Vector3.up) > MaxGroundAngle) continue;
            groundContact++;
            if (rb.linearVelocity.y <= 0.1f) isJumping = false;
            return;
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
}
