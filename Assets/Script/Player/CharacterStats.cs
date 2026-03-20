using Mirror;
using UnityEngine;

/// <summary>
/// 캐릭터 기본 스탯 + 강화 시스템
/// 언리얼 UAttributeSet 대응
/// 
/// 데미지 계산 구조:
/// finalAttack = baseAttack * (1 + upgradeLevel * upgradeBonus)
/// WeaponBase.damageMultiplier 는 무기별 배율 (평타 1.0, 스킬 2.5 등)
/// → 어떤 무기든 강화 레벨만 올리면 자동 반영
/// </summary>
public class CharacterStats : NetworkBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseAttack  = 20f;
    [SerializeField] private float baseMaxHp   = 100f;
    [SerializeField] private float baseDefense = 5f;

    [Header("Upgrade")]
    // 강화 1레벨당 공격력 10%, HP 8%, 방어력 5% 증가
    [SerializeField] private float attackUpgradeBonus  = 0.10f;
    [SerializeField] private float hpUpgradeBonus      = 0.08f;
    [SerializeField] private float defenseUpgradeBonus = 0.05f;

    // 강화 레벨 — 서버에서 변경, 모든 클라이언트 동기화
    [SyncVar(hook = nameof(OnUpgradeLevelChanged))]
    private int upgradeLevel = 0;

    // ─── 계산된 최종 스탯 (프로퍼티) ─────────────────────────────────────
    public float FinalAttack  => baseAttack  * (1f + upgradeLevel * attackUpgradeBonus);
    public float FinalMaxHp   => baseMaxHp   * (1f + upgradeLevel * hpUpgradeBonus);
    public float FinalDefense => baseDefense * (1f + upgradeLevel * defenseUpgradeBonus);

    // 현재 강화 레벨 (읽기 전용)
    public int UpgradeLevel => upgradeLevel;

    // 스탯 변경 이벤트 — UI나 다른 컴포넌트에서 구독
    public System.Action<int> OnUpgraded;

    // ─── 강화 (서버 전용) ─────────────────────────────────────────────────
    [Server]
    public void Upgrade()
    {
        upgradeLevel++;
        Debug.Log($"[STATS] {gameObject.name} 강화 Lv.{upgradeLevel} " +
                  $"| ATK={FinalAttack:F1} | HP={FinalMaxHp:F1} | DEF={FinalDefense:F1}");
    }

    [Server]
    public void SetUpgradeLevel(int level)
    {
        upgradeLevel = Mathf.Max(0, level);
    }

    // ─── SyncVar Hook ─────────────────────────────────────────────────────
    private void OnUpgradeLevelChanged(int _, int newLevel)
    {
        OnUpgraded?.Invoke(newLevel);
    }
}
