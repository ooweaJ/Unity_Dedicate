using Mirror;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    // 인스펙터에서 캐릭터 SO 연결
    [SerializeField] private CharacterDataSO characterData;

    // SO에서 기본값 가져옴
    private float baseAttack => characterData != null ? characterData.baseAttack : 20f;
    private float baseMaxHp => characterData != null ? characterData.baseMaxHp : 100f;
    private float baseDefense => characterData != null ? characterData.baseDefense : 5f;

    private float attackUpgradeBonus => characterData?.attackUpgradeBonus ?? 0.10f;
    private float hpUpgradeBonus => characterData?.hpUpgradeBonus ?? 0.08f;
    private float defenseUpgradeBonus => characterData?.defenseUpgradeBonus ?? 0.05f;

    [SyncVar(hook = nameof(OnUpgradeLevelChanged))]
    private int upgradeLevel = 0;

    public float FinalAttack => baseAttack * (1f + upgradeLevel * attackUpgradeBonus);
    public float FinalMaxHp => baseMaxHp * (1f + upgradeLevel * hpUpgradeBonus);
    public float FinalDefense => baseDefense * (1f + upgradeLevel * defenseUpgradeBonus);
    public int UpgradeLevel => upgradeLevel;

    public System.Action<int> OnUpgraded;

    [Server] public void Upgrade() => upgradeLevel++;
    [Server] public void SetLevel(int lv) => upgradeLevel = Mathf.Max(0, lv);

    private void OnUpgradeLevelChanged(int _, int newLevel)
        => OnUpgraded?.Invoke(newLevel);
}