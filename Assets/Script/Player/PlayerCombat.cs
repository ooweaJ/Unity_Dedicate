using Mirror;
using UnityEngine;

/// <summary>
/// 공격 입력 처리 및 무기 교체 관리
/// 실제 데미지 계산은 WeaponBase → CharacterStats 위임
/// 
/// 강화 흐름:
/// CharacterStats.Upgrade() → FinalAttack 증가
/// → WeaponBase.GetFinalDamage()가 자동으로 올라간 값 반환
/// → 별도 수정 없이 모든 무기(Melee/Projectile)에 반영
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    [Header("Weapon")]
    [Tooltip("인스펙터에서 MeleeWeapon 또는 ProjectileWeapon 컴포넌트 연결")]
    [SerializeField] private WeaponBase primaryWeapon;

    [Header("Skill")]
    [Tooltip("스킬 무기 (평타와 다른 WeaponBase 파생 클래스 연결 가능)")]
    [SerializeField] private WeaponBase skillWeapon;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // 인스펙터 연결 없으면 자식에서 자동 탐색
        if (primaryWeapon == null)
            primaryWeapon = GetComponentInChildren<WeaponBase>();
    }

    private bool IsControllable() =>
        playerController.localMode || isLocalPlayer;

    // ─── 입력 진입점 (PlayerController InputAction에서 호출) ──────────────
    public void OnAttackInput()
    {
        if (!IsControllable()) return;
        primaryWeapon?.Attack(transform.position, transform.forward);
    }

    public void OnSkillInput()
    {
        if (!IsControllable()) return;

        // 스킬 무기가 따로 없으면 기본 무기로 폴백
        WeaponBase weapon = skillWeapon != null ? skillWeapon : primaryWeapon;
        weapon?.Attack(transform.position, transform.forward);
    }

    // ─── 무기 런타임 교체 (아이템 획득, 캐릭터 변경 등) ──────────────────
    public void EquipPrimary(WeaponBase weapon) => primaryWeapon = weapon;
    public void EquipSkill(WeaponBase weapon)   => skillWeapon   = weapon;

    private void Update()
    {

    }
}
