using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 전담 컴포넌트 — InputAction 소유 및 이벤트 발행
/// PlayerMovement, PlayerCombat 등이 이벤트를 구독해서 처리
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Tooltip("네트워크 없이 로컬 단독 테스트 시 true")]
    public bool localMode = false;

    public event Action<Vector2> OnMove;
    public event Action          OnJump;
    public event Action          OnAttack;
    public event Action          OnSkill1;
    public event Action          OnSkill2;

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
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        jumpAction.performed += _ => { if (IsControllable) OnJump?.Invoke(); };

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.performed += _ => { if (IsControllable) OnAttack?.Invoke(); };

        skill1Action = new InputAction("Skill1", InputActionType.Button);
        skill1Action.AddBinding("<Keyboard>/q");
        skill1Action.AddBinding("<Gamepad>/rightShoulder");
        skill1Action.performed += _ => { if (IsControllable) OnSkill1?.Invoke(); };

        skill2Action = new InputAction("Skill2", InputActionType.Button);
        skill2Action.AddBinding("<Keyboard>/e");
        skill2Action.AddBinding("<Gamepad>/leftShoulder");
        skill2Action.performed += _ => { if (IsControllable) OnSkill2?.Invoke(); };
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        attackAction.Enable();
        skill1Action.Enable();
        skill2Action.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();
        skill1Action.Disable();
        skill2Action.Disable();
    }
}
