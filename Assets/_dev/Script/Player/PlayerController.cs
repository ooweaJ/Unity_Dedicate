using Mirror;
using UnityEngine;

/// <summary>
/// Mediator — 플레이어 컴포넌트 간 연결 배선 담당
/// "어떤 입력이 어떤 컴포넌트로 가는지"를 여기서만 확인하면 됨
///
/// PlayerInputHandler : 입력 발행
/// PlayerMovement     : 이동/점프/대시 처리
/// PlayerCombat       : 공격/스킬 처리
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
