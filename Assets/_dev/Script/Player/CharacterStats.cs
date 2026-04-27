using Mirror;
using UnityEngine;

/// <summary>
/// 캐릭터 스탯 관리
/// 배틀 서버에서는 BattleNetworkPlayer로부터 전달받은 최종 스탯을 그대로 적용합니다.
/// </summary>
public class CharacterStats : NetworkBehaviour
{
    [SerializeField] private CharacterDataSO characterData;

    [SyncVar(hook = nameof(OnStatChanged))] private float finalMaxHp;
    [SyncVar] private float finalAtk;
    [SyncVar] private float finalDef;

    public float FinalMaxHp   => finalMaxHp;
    public float FinalAttack  => finalAtk;
    public float FinalDefense => finalDef;

    public System.Action OnStatsApplied;

    [Server]
    public void SetData(CharacterDataSO data)
    {
        characterData = data;
    }

    [Server]
    public void ApplyStats(CharacterStatData stats)
    {
        finalMaxHp = stats.maxHp;
        finalAtk   = stats.atk;
        finalDef   = stats.def;
        
        OnStatsApplied?.Invoke();
    }

    private void OnStatChanged(float oldVal, float newVal)
    {
        OnStatsApplied?.Invoke();
    }
}
