using System;
using Mirror;
using UnityEngine;

/// <summary>
/// 씬에 하나 배치 — 조이스틱 이벤트를 IPlayerInputProvider로 노출
/// PC 빌드: 이동 조이스틱 숨김, 스킬 버튼은 쿨다운 뷰어로만 사용 (knob 숨김)
/// </summary>
public class MobileInputProvider : MonoBehaviour, IPlayerInputProvider
{
    [SerializeField] private VirtualMovementJoystick movementJoystick;
    [SerializeField] private VirtualJoystickButton   basicJoystick;
    [SerializeField] private VirtualJoystickButton   skill1Joystick;
    [SerializeField] private VirtualJoystickButton   skill2Joystick;

    public event Action<Vector2> OnMove;
    public event Action          OnJump;
    public event Action<int>                          OnSkillDown;
    public event Action<int, Vector2, float, Vector3> OnSkillReleased;

    private CharacterWeapon _weapon;
    private PlayerCombat    _combat;

    private void Awake()
    {
        ApplyPlatformUI();
        BindJoystickEvents();
    }

    private void ApplyPlatformUI()
    {
#if UNITY_EDITOR
        bool isMobile = true;
#else
        bool isMobile = Application.isMobilePlatform;
#endif
        if (movementJoystick != null)
            movementJoystick.gameObject.SetActive(isMobile);

        basicJoystick ?.SetPCMode(!isMobile);
        skill1Joystick?.SetPCMode(!isMobile);
        skill2Joystick?.SetPCMode(!isMobile);
    }

    private void BindJoystickEvents()
    {
        if (movementJoystick != null)
            movementJoystick.OnMove += dir => OnMove?.Invoke(dir);

        BindSkillJoystick(basicJoystick,  0);
        BindSkillJoystick(skill1Joystick, 1);
        BindSkillJoystick(skill2Joystick, 2);
    }

    private void BindSkillJoystick(VirtualJoystickButton joystick, int slot)
    {
        if (joystick == null) return;
        joystick.OnPress      += ()         => OnSkillDown?.Invoke(slot);
        joystick.OnDragUpdate += (dir, mag) => _combat?.HandleMobileSkillDrag(slot, dir, mag);
        joystick.OnFire       += (dir, mag) => OnSkillReleased?.Invoke(slot, dir, mag, Vector3.zero);
    }

    private void Update()
    {
        TryBindWeapon();
        UpdateCooldowns();
    }

    private void TryBindWeapon()
    {
        if (_weapon != null) return;
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;
        _weapon = localPlayer.GetComponent<CharacterWeapon>();
        _combat = localPlayer.GetComponent<PlayerCombat>();
    }

    private void UpdateCooldowns()
    {
        if (_weapon == null) return;
        UpdateJoystickCooldown(basicJoystick,  0);
        UpdateJoystickCooldown(skill1Joystick, 1);
        UpdateJoystickCooldown(skill2Joystick, 2);
    }

    private void UpdateJoystickCooldown(VirtualJoystickButton joystick, int slot)
    {
        if (joystick == null) return;
        float ratio = _weapon.GetCooldownRatio(slot);
        joystick.SetCooldownRatio(ratio);
        joystick.SetBlocked(ratio > 0f);
    }
}
