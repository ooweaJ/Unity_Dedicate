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

    private Action                          _onAttackDown;
    private Action<Vector2, float, Vector3> _onAttackUp;
    private Action                          _onSkill1Down;
    private Action<Vector2, float, Vector3> _onSkill1Up;
    private Action                          _onSkill2Down;
    private Action<Vector2, float, Vector3> _onSkill2Up;

    private void Awake()
    {
        input    = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        combat   = GetComponent<PlayerCombat>();

        _onAttackDown = ()                   => combat.HandleSkillDown(0);
        _onAttackUp   = (dir, mag, pos)      => combat.HandleSkillReleased(0, dir, mag, pos);
        _onSkill1Down = ()                   => combat.HandleSkillDown(1);
        _onSkill1Up   = (dir, mag, pos)      => combat.HandleSkillReleased(1, dir, mag, pos);
        _onSkill2Down = ()                   => combat.HandleSkillDown(2);
        _onSkill2Up   = (dir, mag, pos)      => combat.HandleSkillReleased(2, dir, mag, pos);

        input.OnMove      += movement.HandleMove;
        input.OnJump      += movement.HandleJump;
        input.OnAttackDown += _onAttackDown;
        input.OnAttackUp   += _onAttackUp;
        input.OnSkill1Down += _onSkill1Down;
        input.OnSkill1Up   += _onSkill1Up;
        input.OnSkill2Down += _onSkill2Down;
        input.OnSkill2Up   += _onSkill2Up;
    }

    private void OnDestroy()
    {
        if (input == null) return;
        input.OnMove      -= movement.HandleMove;
        input.OnJump      -= movement.HandleJump;
        input.OnAttackDown -= _onAttackDown;
        input.OnAttackUp   -= _onAttackUp;
        input.OnSkill1Down -= _onSkill1Down;
        input.OnSkill1Up   -= _onSkill1Up;
        input.OnSkill2Down -= _onSkill2Down;
        input.OnSkill2Up   -= _onSkill2Up;
    }
}
