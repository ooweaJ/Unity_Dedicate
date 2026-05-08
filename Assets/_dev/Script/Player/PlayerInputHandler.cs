using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PC 입력 전담 — 이벤트 발행
/// 모바일은 MobileCombatUI가 PlayerCombat을 직접 호출
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Tooltip("네트워크 없이 로컬 단독 테스트 시 true")]
    public bool localMode = false;

    public event Action<Vector2> OnMove;
    public event Action          OnJump;

    // Vector2 = XZ 평면 조준 방향(normalized), float = magnitude, Vector3 = 마우스 월드 좌표(PC) or zero(모바일)
    public event Action<Vector2, float, Vector3> OnAttack;
    public event Action<Vector2, float, Vector3> OnSkill1;
    public event Action<Vector2, float, Vector3> OnSkill2;

    private NetworkIdentity netIdentity;
    private InputAction moveAction, jumpAction, attackAction, skill1Action, skill2Action;

    private bool IsControllable => localMode || (netIdentity != null && netIdentity.isLocalPlayer);

    private void Awake()
    {
        netIdentity = GetComponent<NetworkIdentity>();
        RegisterActions();
    }

    private void Update()
    {
        if (!IsControllable) return;
        OnMove?.Invoke(moveAction.ReadValue<Vector2>());
    }

    private void RegisterActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.performed += _ => { if (IsControllable) OnJump?.Invoke(); };

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.performed += _ => { if (IsControllable) OnAttack?.Invoke(GetMouseAimDir(), 1f, GetMouseWorldPos()); };

        skill1Action = new InputAction("Skill1", InputActionType.Button);
        skill1Action.AddBinding("<Keyboard>/q");
        skill1Action.performed += _ => { if (IsControllable) OnSkill1?.Invoke(GetMouseAimDir(), 1f, GetMouseWorldPos()); };

        skill2Action = new InputAction("Skill2", InputActionType.Button);
        skill2Action.AddBinding("<Keyboard>/e");
        skill2Action.performed += _ => { if (IsControllable) OnSkill2?.Invoke(GetMouseAimDir(), 1f, GetMouseWorldPos()); };
    }

    // 마우스 위치 → 캐릭터 기준 XZ 방향
    private Vector2 GetMouseAimDir()
    {
        if (Camera.main == null) return new Vector2(transform.forward.x, transform.forward.z);

        var plane = new Plane(Vector3.up, transform.position);
        var ray   = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (plane.Raycast(ray, out float dist))
        {
            Vector3 worldPos = ray.GetPoint(dist);
            Vector3 dir = worldPos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                return new Vector2(dir.x, dir.z).normalized;
        }
        return new Vector2(transform.forward.x, transform.forward.z);
    }

    // 마우스 위치 → 월드 좌표 (y=캐릭터 기준 수평면)
    private Vector3 GetMouseWorldPos()
    {
        if (Camera.main == null) return Vector3.zero;

        var plane = new Plane(Vector3.up, transform.position);
        var ray   = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);

        return Vector3.zero;
    }

    private void OnEnable()
    {
        moveAction.Enable();  jumpAction.Enable();
        attackAction.Enable(); skill1Action.Enable(); skill2Action.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();  jumpAction.Disable();
        attackAction.Disable(); skill1Action.Disable(); skill2Action.Disable();
    }
}
