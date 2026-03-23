using Mirror;
using UnityEngine;

/// <summary>
/// 공격 입력 진입점
/// 실제 판정은 CharacterWeapon에 위임
/// PlayerController의 InputAction에서 호출됨
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    private CharacterWeapon  weapon;
    private PlayerController playerCtrl;

    private void Awake()
    {
        weapon     = GetComponent<CharacterWeapon>();
        playerCtrl = GetComponent<PlayerController>();
    }

    private bool IsControllable() => isLocalPlayer;

    public void OnAttackInput()
    {
        if (!IsControllable()) return;
        weapon?.UseBasicAttack();
    }

    public void OnSkillInput()
    {
        if (!IsControllable()) return;
        weapon?.UseSkillAttack();
    }
}
