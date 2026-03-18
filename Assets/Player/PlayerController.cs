using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mirror 이동 동기화 구조:
/// 클라이언트 입력 → Rigidbody 이동 → NetworkTransform(ClientAuthority)이 서버로 위치 전송
/// → 서버가 다른 클라이언트에게 브로드캐스트
/// 언리얼 CharacterMovementComponent와 같은 역할
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 720f;

    [Header("Animation")]
    public Animator animator;

    [Header("Debug")]
    [Tooltip("true = 서버 없이 에디터 단독 테스트")]
    public bool localMode = false;

    private Rigidbody rb;
    private Vector2 moveInput;
    private InputAction moveAction;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // New Input System - WASD + 방향키 + 게임패드
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
    }

    void OnEnable()  { moveAction.Enable(); }
    void OnDisable() { moveAction.Disable(); }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log("[PLAYER] 로컬 플레이어 시작");
    }

    bool IsControllable()
    {
        if (localMode) return true;
        return isLocalPlayer;
    }

    void Update()
    {
        if (!IsControllable()) return;
        moveInput = moveAction.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        if (!IsControllable()) return;
        Move();
        Animate();
    }

    void Move()
    {
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);

        if (dir.magnitude < 0.1f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        // 이동
        rb.linearVelocity = new Vector3(
            dir.x * moveSpeed,
            rb.linearVelocity.y,
            dir.z * moveSpeed
        );

        // 이동 방향으로 회전
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(dir),
            rotateSpeed * Time.fixedDeltaTime
        );
    }

    void Animate()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", moveInput.magnitude);
    }
}
