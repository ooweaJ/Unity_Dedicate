using Mirror;
using UnityEngine;

/// <summary>
/// 캐릭터 스탯 관리
/// CharacterDataSO를 인스펙터에서 연결하거나
/// CharacterInitializer.OnStartServer()에서 SetData()로 런타임 주입
/// </summary>
public class CharacterStats : NetworkBehaviour
{
    [SerializeField] private CharacterDataSO characterData;

    private float baseAttack          => characterData != null ? characterData.baseAttack          : 20f;
    private float baseMaxHp           => characterData != null ? characterData.baseMaxHp           : 100f;
    private float baseDefense         => characterData != null ? characterData.baseDefense         : 5f;
    private float attackUpgradeBonus  => characterData?.attackUpgradeBonus  ?? 0.10f;
    private float hpUpgradeBonus      => characterData?.hpUpgradeBonus      ?? 0.08f;
    private float defenseUpgradeBonus => characterData?.defenseUpgradeBonus ?? 0.05f;

    [SyncVar(hook = nameof(OnUpgradeLevelChanged))]
    private int upgradeLevel = 0;

    public float FinalAttack  => baseAttack  * (1f + upgradeLevel * attackUpgradeBonus);
    public float FinalMaxHp   => baseMaxHp   * (1f + upgradeLevel * hpUpgradeBonus);
    public float FinalDefense => baseDefense * (1f + upgradeLevel * defenseUpgradeBonus);
    public int   UpgradeLevel => upgradeLevel;

    public System.Action<int> OnUpgraded;

    // ─── CharacterInitializer에서 런타임 주입 ─────────────────────────
    [Server]
    public void SetData(CharacterDataSO data)
    {
        characterData = data;
    }

    [Server] public void Upgrade()         => upgradeLevel++;
    [Server] public void SetLevel(int lv)  => upgradeLevel = Mathf.Max(0, lv);

    private void OnUpgradeLevelChanged(int _, int newLevel)
        => OnUpgraded?.Invoke(newLevel);
}
