using Mirror;
using UnityEngine;

/// <summary>
/// 공격/스킬 입력 처리 → CharacterWeapon 위임
/// PC:     PlayerController가 PlayerInputHandler 이벤트를 여기로 연결
/// 모바일: MobileCombatUI가 직접 호출
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    private CharacterWeapon weapon;

    private void Awake()
    {
        weapon = GetComponent<CharacterWeapon>();
    }

    // ─── PlayerController / MobileCombatUI 가 호출 ───────────────────────
    public void HandleAttack(Vector2 aimDir2D, float magnitude = 1f, Vector3 worldPos = default)
        => weapon?.UseBasicAttack(ToAimDir3D(aimDir2D), magnitude, worldPos);

    public void HandleSkill1(Vector2 aimDir2D, float magnitude = 1f, Vector3 worldPos = default)
        => weapon?.UseSkill1Attack(ToAimDir3D(aimDir2D), magnitude, worldPos);

    public void HandleSkill2(Vector2 aimDir2D, float magnitude = 1f, Vector3 worldPos = default)
        => weapon?.UseSkill2Attack(ToAimDir3D(aimDir2D), magnitude, worldPos);

    // XZ 평면 2D 방향 → 월드 3D 방향
    private static Vector3 ToAimDir3D(Vector2 dir2D)
        => new Vector3(dir2D.x, 0f, dir2D.y).normalized;
}
