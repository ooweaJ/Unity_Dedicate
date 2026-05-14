using Mirror;
using UnityEngine;

/// <summary>
/// 공격/스킬 입력 처리 → CharacterWeapon 위임
///
/// PC:     PlayerController → HandleSkillDown / HandleSkillReleased (슬롯 0/1/2)
/// 모바일: MobileInputProvider → 동일 경로 (IPlayerInputProvider 통해 PlayerController로)
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    private CharacterWeapon         weapon;
    private GrenadeAimIndicator     aimIndicator;
    private SkillDirectionIndicator dirIndicator;

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

        dirIndicator = GetComponentInChildren<SkillDirectionIndicator>();
        if (dirIndicator == null)
        {
            var go = new GameObject("SkillDirectionIndicator");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            dirIndicator = go.AddComponent<SkillDirectionIndicator>();
        }

        dirIndicator.Show(transform);
    }

    // ─── PlayerController 호출 ────────────────────────────────────────────

    public void HandleSkillDown(int slot)
    {
        if (weapon == null) return;
        if (weapon.GetCooldownRatio(slot) > 0f) return;

        if (weapon.IsTargetPointSkill(slot))
        {
            if (aimIndicator == null) return;
            var (maxDist, radius) = weapon.GetExplosiveSkillInfo(slot);
            aimIndicator.BeginAim(maxDist, radius);
        }
        // dirIndicator는 항상 보이므로 별도 처리 없음
    }

    public void HandleSkillReleased(int slot, Vector2 aimDir2D, float magnitude, Vector3 worldPos)
    {
        dirIndicator?.ClearOverride();

        if (weapon == null || weapon.GetCooldownRatio(slot) > 0f)
        {
            aimIndicator?.EndAim();
            return;
        }

        // dir이 zero면 탭(방향 미지정) → 캐릭터 정면으로 발사
        Vector3 aimDir   = aimDir2D.sqrMagnitude > 0.01f ? ToAimDir3D(aimDir2D) : transform.forward;
        float   finalMag = magnitude > 0.01f ? magnitude : 1f;

        if (aimIndicator != null && aimIndicator.IsActive)
        {
            FireSkill(slot, aimDir, finalMag, aimIndicator.TargetPos);
            aimIndicator.EndAim();
        }
        else
        {
            FireSkill(slot, aimDir, finalMag, worldPos);
        }
    }

    // ─── 모바일 드래그 중 인디케이터 갱신 ────────────────────────────────

    public void HandleMobileSkillDrag(int slot, Vector2 joystickDir, float magnitude)
    {
        if (weapon == null) return;
        Vector3 dir3D = new Vector3(joystickDir.x, 0f, joystickDir.y);

        if (weapon.IsTargetPointSkill(slot))
        {
            if (aimIndicator == null || !aimIndicator.IsActive) return;
            var (maxDist, _) = weapon.GetExplosiveSkillInfo(slot);
            aimIndicator.SetDirectTarget(transform.position + dir3D * (magnitude * maxDist));
        }
        else if (magnitude > 0.1f)
        {
            dirIndicator?.SetOverrideDirection(dir3D);
        }
    }

    // ─── 구형 호환 ────────────────────────────────────────────────────────

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

    private static bool IsMobileMode()
    {
#if UNITY_EDITOR
        return true;
#else
        return Application.isMobilePlatform;
#endif
    }
}
