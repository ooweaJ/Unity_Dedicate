using Mirror;
using UnityEngine;

/// <summary>
/// Mediator — 플레이어 컴포넌트 간 연결 배선 담당
/// PC: PlayerInputHandler → PlayerCombat
/// 모바일: MobileCombatUI → PlayerCombat (직접 호출, 여기서 관리 안 함)
/// </summary>
public class PlayerController : NetworkBehaviour
{
    private PlayerInputHandler input;
    private PlayerMovement     movement;
    private PlayerCombat       combat;

    private void Awake()
    {
        input    = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        combat   = GetComponent<PlayerCombat>();

        input.OnMove   += movement.HandleMove;
        input.OnJump   += movement.HandleJump;
        input.OnAttack += combat.HandleAttack;
        input.OnSkill1 += combat.HandleSkill1;
        input.OnSkill2 += combat.HandleSkill2;
    }

    private void OnDestroy()
    {
        if (input == null) return;
        input.OnMove   -= movement.HandleMove;
        input.OnJump   -= movement.HandleJump;
        input.OnAttack -= combat.HandleAttack;
        input.OnSkill1 -= combat.HandleSkill1;
        input.OnSkill2 -= combat.HandleSkill2;
    }
}
