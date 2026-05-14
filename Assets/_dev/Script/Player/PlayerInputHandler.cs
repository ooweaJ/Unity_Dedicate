using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : NetworkBehaviour, IPlayerInputProvider
{
    [Tooltip("네트워크 없이 로컬 단독 테스트 시 true")]
    public bool localMode = false;

    public event Action<Vector2> OnMove;
    public event Action          OnJump;
    public event Action<int>                          OnSkillDown;
    public event Action<int, Vector2, float, Vector3> OnSkillReleased;

    private NetworkIdentity netIdentity;
    private InputAction moveAction, jumpAction, attackAction, skill1Action, skill2Action;

    private bool IsControllable => localMode || (netIdentity != null && netIdentity.isLocalPlayer);

    private void Awake()
    {
        if (Application.isMobilePlatform) return;
        netIdentity = GetComponent<NetworkIdentity>();
        RegisterActions();
    }

    private void Update()
    {
        if (Application.isMobilePlatform || !IsControllable) return;
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
        attackAction.started  += _ => { if (IsControllable) OnSkillDown?.Invoke(0); };
        attackAction.canceled += _ => { if (IsControllable) OnSkillReleased?.Invoke(0, GetMouseAimDir(), 1f, GetMouseWorldPos()); };

        skill1Action = new InputAction("Skill1", InputActionType.Button);
        skill1Action.AddBinding("<Keyboard>/q");
        skill1Action.started  += _ => { if (IsControllable) OnSkillDown?.Invoke(1); };
        skill1Action.canceled += _ => { if (IsControllable) OnSkillReleased?.Invoke(1, GetMouseAimDir(), 1f, GetMouseWorldPos()); };

        skill2Action = new InputAction("Skill2", InputActionType.Button);
        skill2Action.AddBinding("<Keyboard>/e");
        skill2Action.started  += _ => { if (IsControllable) OnSkillDown?.Invoke(2); };
        skill2Action.canceled += _ => { if (IsControllable) OnSkillReleased?.Invoke(2, GetMouseAimDir(), 1f, GetMouseWorldPos()); };
    }

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
        moveAction?.Enable(); jumpAction?.Enable();
        attackAction?.Enable(); skill1Action?.Enable(); skill2Action?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable(); jumpAction?.Disable();
        attackAction?.Disable(); skill1Action?.Disable(); skill2Action?.Disable();
    }
}
