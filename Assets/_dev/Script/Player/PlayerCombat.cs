using Mirror;
using UnityEngine;

/// <summary>
/// 공격/스킬 입력 처리 → CharacterWeapon 위임
///
/// PC:     PlayerController → HandleSkillDown / HandleSkillReleased (슬롯 0/1/2)
/// 모바일: MobileCombatUI  → HandleAttack / HandleSkill1 / HandleSkill2 (직접 호출)
///
/// 모든 스킬 공통 흐름 (PC):
///   버튼 Down  → HandleSkillDown(slot)  : TargetPoint면 조준 인디케이터 표시
///   버튼 Up    → HandleSkillReleased(slot): 인디케이터 위치 or 마우스 방향으로 발사
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    private CharacterWeapon     weapon;
    private GrenadeAimIndicator aimIndicator;

    private void Awake()
    {
        weapon = GetComponent<CharacterWeapon>();
    }

    public override void OnStartLocalPlayer()
    {
        aimIndicator = GetComponentInChildren<GrenadeAimIndicator>();
        if (aimIndicator == null)
        {
            var go = new GameObject("GrenadeAimIndicator");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            aimIndicator = go.AddComponent<GrenadeAimIndicator>();
        }
    }

    // ─── PC: PlayerController 호출 ────────────────────────────────────────

    /// <summary>버튼 Down — TargetPoint 스킬이면 조준 인디케이터 표시</summary>
    public void HandleSkillDown(int slot)
    {
        if (weapon == null || aimIndicator == null) return;
        if (weapon.IsTargetPointSkill(slot))
        {
            var (maxDist, radius) = weapon.GetExplosiveSkillInfo(slot);
            aimIndicator.BeginAim(maxDist, radius);
        }
    }

    /// <summary>버튼 Up — TargetPoint면 인디케이터 위치로, Directional이면 마우스 방향으로 발사</summary>
    public void HandleSkillReleased(int slot, Vector2 aimDir2D, float magnitude, Vector3 worldPos)
    {
        if (aimIndicator != null && aimIndicator.IsActive)
        {
            FireSkill(slot, ToAimDir3D(aimDir2D), magnitude, aimIndicator.TargetPos);
            aimIndicator.EndAim();
        }
        else
        {
            FireSkill(slot, ToAimDir3D(aimDir2D), magnitude, worldPos);
        }
    }

    // ─── 모바일 MobileCombatUI 직접 호출 ─────────────────────────────────

    public void HandleAttack(Vector2 aimDir2D, float magnitude = 1f, Vector3 worldPos = default)
        => FireSkill(0, ToAimDir3D(aimDir2D), magnitude, worldPos);

    public void HandleSkill1(Vector2 aimDir2D, float magnitude = 1f, Vector3 worldPos = default)
        => FireSkill(1, ToAimDir3D(aimDir2D), magnitude, worldPos);

    public void HandleSkill2(Vector2 aimDir2D, float magnitude = 1f, Vector3 worldPos = default)
        => FireSkill(2, ToAimDir3D(aimDir2D), magnitude, worldPos);

    // ─── 공통 ─────────────────────────────────────────────────────────────

    private void FireSkill(int slot, Vector3 aimDir, float magnitude, Vector3 worldPos)
    {
        switch (slot)
        {
            case 0: weapon?.UseBasicAttack(aimDir, magnitude, worldPos);  break;
            case 1: weapon?.UseSkill1Attack(aimDir, magnitude, worldPos); break;
            case 2: weapon?.UseSkill2Attack(aimDir, magnitude, worldPos); break;
        }
    }

    private static Vector3 ToAimDir3D(Vector2 dir2D)
        => new Vector3(dir2D.x, 0f, dir2D.y).normalized;
}
