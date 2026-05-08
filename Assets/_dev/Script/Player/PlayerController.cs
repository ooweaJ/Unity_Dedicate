using System;
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

    private Action<Vector2, float, Vector3> _onAttack;
    private Action<Vector2, float, Vector3> _onSkill1;
    private Action<Vector2, float, Vector3> _onSkill2;

    private void Awake()
    {
        input    = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        combat   = GetComponent<PlayerCombat>();

        _onAttack = (dir, mag, pos) => combat.HandleAttack(dir, mag, pos);
        _onSkill1 = (dir, mag, pos) => combat.HandleSkill1(dir, mag, pos);
        _onSkill2 = (dir, mag, pos) => combat.HandleSkill2(dir, mag, pos);

        input.OnMove   += movement.HandleMove;
        input.OnJump   += movement.HandleJump;
        input.OnAttack += _onAttack;
        input.OnSkill1 += _onSkill1;
        input.OnSkill2 += _onSkill2;
    }

    private void OnDestroy()
    {
        if (input == null) return;
        input.OnMove   -= movement.HandleMove;
        input.OnJump   -= movement.HandleJump;
        input.OnAttack -= _onAttack;
        input.OnSkill1 -= _onSkill1;
        input.OnSkill2 -= _onSkill2;
    }
}
