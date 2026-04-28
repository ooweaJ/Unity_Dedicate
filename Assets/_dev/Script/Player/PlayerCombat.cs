using Mirror;
using UnityEngine;

/// <summary>
/// 공격/스킬 입력 처리 → CharacterWeapon 위임
/// 이벤트 구독은 PlayerController(Mediator)가 담당
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    private CharacterWeapon weapon;

    private void Awake()
    {
        weapon = GetComponent<CharacterWeapon>();
    }

    // ─── PlayerController가 연결하는 핸들러 ──────────────────────────────
    public void HandleAttack() => weapon?.UseBasicAttack();
    public void HandleSkill1() => weapon?.UseSkill1Attack();
    public void HandleSkill2() => weapon?.UseSkill2Attack();
}
