using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 720f;

    [Header("Jump")]
    public float jumpForce = 6f;

    [Header("Debug")]
    public bool localMode = false;

    // ─── 컴포넌트 ────────────────────────────────────────────────────────
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerAnimationController animController;
    private PlayerCombat combat;

    // ─── 입력 ────────────────────────────────────────────────────────────
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction skillAction;

    private Vector2 moveInput;

    // ─── 지면 판정 ───────────────────────────────────────────────────────
    // 언리얼 CMC와 동일 방식 : 충돌 노멀이 위를 향하면 지면
    private bool isGrounded = false;
    private bool isJumping = false;
    private int groundContact = 0; // 동시에 닿은 지면 콜라이더 수

    // 지면으로 인정할 최소 노멀 각도 (언리얼 WalkableFloorAngle 대응)
    // 45도 이하 경사만 지면으로 인정
    private const float MaxGroundAngle = 45f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        animController = GetComponent<PlayerAnimationController>();
        combat = GetComponent<PlayerCombat>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        InitInputActions();
    }

    // ─── 충돌 이벤트 : 언리얼 CMC 지면 판정과 동일 원리 ─────────────────
    // OnCollisionStay : 캡슐이 무언가에 닿아있는 매 물리 프레임
    private void OnCollisionStay(Collision col)
    {
        // 점프 직후엔 지면 판정 무시
        if (isJumping) return;

        foreach (ContactPoint contact in col.contacts)
        {
            float angle = Vector3.Angle(contact.normal, Vector3.up);
            if (angle <= MaxGroundAngle)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision col)
    {
        groundContact--;
        if (groundContact <= 0)
        {
            groundContact = 0;
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        foreach (ContactPoint contact in col.contacts)
        {
            float angle = Vector3.Angle(contact.normal, Vector3.up);
            if (angle <= MaxGroundAngle)
            {
                groundContact++;

                // 착지 순간 점프 플래그 해제
                if (rb.linearVelocity.y <= 0.1f)
                    isJumping = false;

                return;
            }
        }
    }

    // ─── Update : 입력 읽기 ──────────────────────────────────────────────
    private void Update()
    {
        if (!IsControllable()) return;
        moveInput = moveAction.ReadValue<Vector2>();

    }

    // ─── FixedUpdate : 물리 처리 (언리얼 TickComponent 물리 파트) ────────
    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        Move();
        SyncAnimationParams();
    }

    private void Move()
    {
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);

        if (dir.magnitude < 0.1f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        rb.linearVelocity = new Vector3(
            dir.x * moveSpeed,
            rb.linearVelocity.y,
            dir.z * moveSpeed
        );

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(dir),
            rotateSpeed * Time.fixedDeltaTime
        );
    }

    // ─── 애니메이션 파라미터 동기화 ──────────────────────────────────────
    private void SyncAnimationParams()
    {
        if (animController == null) return;

        float speed = moveInput.magnitude;
        float direction = CalculateDirection();
        float jumpH = isGrounded ? 0f : Mathf.Max(0f, rb.linearVelocity.y);
        float gravCtrl = isGrounded ? 0f : rb.linearVelocity.y;
        bool jump = !isGrounded;
        bool rest = speed < 0.05f && isGrounded;

        animController.UpdateMovementParams(
            speed, direction, jumpH, gravCtrl, jump, rest);
    }

    private float CalculateDirection()
    {
        if (moveInput.magnitude < 0.1f) return 0f;
        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
        float angle = Vector3.SignedAngle(transform.forward, moveDir, Vector3.up);
        return Mathf.Clamp(angle / 180f, -1f, 1f);
    }

    // ─── 점프 : 입력 이벤트에서 즉시 처리 ───────────────────────────────
    private void OnJumpInput()
    {
        if (!IsControllable()) return;
        if (!isGrounded || isJumping) return;

        isJumping = true;
        isGrounded = false; // 즉시 공중 상태

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            jumpForce,
            rb.linearVelocity.z
        );

        animController.UpdateMovementParams(
            moveInput.magnitude,
            CalculateDirection(),
            jumpForce,
            jumpForce,
            jump: true,
            rest: false
        );
    }

    // ─── Input 초기화 ────────────────────────────────────────────────────
    private void InitInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        jumpAction.performed += _ => OnJumpInput();

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.performed += _ => combat?.OnAttackInput();

        skillAction = new InputAction("Skill", InputActionType.Button);
        skillAction.AddBinding("<Keyboard>/q");
        skillAction.performed += _ => combat?.OnSkillInput();
    }

    private bool IsControllable() => localMode || isLocalPlayer;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log("[PLAYER] 로컬 플레이어 시작");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        attackAction.Enable();
        skillAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();
        skillAction.Disable();
    }
}