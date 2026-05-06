using Mirror;
using UnityEngine;

/// <summary>
/// 모바일 전투 UI — 조이스틱 3개를 관리하고 PlayerCombat에 입력을 전달
///
/// 로컬 플레이어를 자동 탐색하므로 씬에 하나만 배치하면 됨
/// 프리팹 구조 예시:
///   MobileCombatUI (Canvas → 이 컴포넌트)
///   ├── BasicJoystick  (VirtualJoystickButton)
///   ├── Skill1Joystick (VirtualJoystickButton)
///   └── Skill2Joystick (VirtualJoystickButton)
/// </summary>
public class MobileCombatUI : MonoBehaviour
{
    [SerializeField] private VirtualJoystickButton basicJoystick;
    [SerializeField] private VirtualJoystickButton skill1Joystick;
    [SerializeField] private VirtualJoystickButton skill2Joystick;

    private PlayerCombat     _combat;
    private CharacterWeapon  _weapon;

    private void Update()
    {
        TryBindLocalPlayer();
        UpdateCooldowns();
    }

    private void TryBindLocalPlayer()
    {
        if (_combat != null) return;

        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        _combat = localPlayer.GetComponent<PlayerCombat>();
        _weapon = localPlayer.GetComponent<CharacterWeapon>();

        if (_combat == null) return;

        if (basicJoystick  != null) basicJoystick .OnFire += _combat.HandleAttack;
        if (skill1Joystick != null) skill1Joystick.OnFire += _combat.HandleSkill1;
        if (skill2Joystick != null) skill2Joystick.OnFire += _combat.HandleSkill2;
    }

    private void UpdateCooldowns()
    {
        if (_weapon == null) return;
        basicJoystick ?.SetCooldownRatio(_weapon.GetCooldownRatio(0));
        skill1Joystick?.SetCooldownRatio(_weapon.GetCooldownRatio(1));
        skill2Joystick?.SetCooldownRatio(_weapon.GetCooldownRatio(2));
    }

    private void OnDestroy()
    {
        if (_combat == null) return;
        if (basicJoystick  != null) basicJoystick .OnFire -= _combat.HandleAttack;
        if (skill1Joystick != null) skill1Joystick.OnFire -= _combat.HandleSkill1;
        if (skill2Joystick != null) skill2Joystick.OnFire -= _combat.HandleSkill2;
    }
}
