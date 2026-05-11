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

    // 모든 스킬 공통: Down = 누른 순간(조준 시작), Up = 뗀 순간(발사)
    // 빠른 공격은 탭(바로 떼기)하면 즉시 발사와 동일하게 동작
    public event Action                          OnAttackDown;
    public event Action<Vector2, float, Vector3> OnAttackUp;

    public event Action                          OnSkill1Down;
    public event Action<Vector2, float, Vector3> OnSkill1Up;

    public event Action                          OnSkill2Down;
    public event Action<Vector2, float, Vector3> OnSkill2Up;

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
        attackAction.started  += _ => { if (IsControllable) OnAttackDown?.Invoke(); };
        attackAction.canceled += _ => { if (IsControllable) OnAttackUp?.Invoke(GetMouseAimDir(), 1f, GetMouseWorldPos()); };

        skill1Action = new InputAction("Skill1", InputActionType.Button);
        skill1Action.AddBinding("<Keyboard>/q");
        skill1Action.started  += _ => { if (IsControllable) OnSkill1Down?.Invoke(); };
        skill1Action.canceled += _ => { if (IsControllable) OnSkill1Up?.Invoke(GetMouseAimDir(), 1f, GetMouseWorldPos()); };

        skill2Action = new InputAction("Skill2", InputActionType.Button);
        skill2Action.AddBinding("<Keyboard>/e");
        skill2Action.started  += _ => { if (IsControllable) OnSkill2Down?.Invoke(); };
        skill2Action.canceled += _ => { if (IsControllable) OnSkill2Up?.Invoke(GetMouseAimDir(), 1f, GetMouseWorldPos()); };
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
