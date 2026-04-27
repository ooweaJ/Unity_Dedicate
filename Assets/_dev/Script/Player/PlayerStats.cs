using Mirror;
using UnityEngine;

public class PlayerStats : NetworkBehaviour, IDamageable
{
    [SyncVar(hook = nameof(OnHpChanged))]
    private float currentHp;

    private CharacterStats charStats;
    private PlayerHPBar    hpBar;

    public bool  IsDead        => currentHp <= 0f;
    public float CurrentHpRatio => GetMaxHp() > 0f ? currentHp / GetMaxHp() : 0f;

    private void Awake()
    {
        charStats = GetComponent<CharacterStats>();
        hpBar     = GetComponent<PlayerHPBar>();
    }

    public override void OnStartServer()
    {
        if (charStats != null)
            charStats.OnStatsApplied += OnStatsAppliedFromServer;

        BattleManager.Instance?.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        if (charStats != null)
            charStats.OnStatsApplied -= OnStatsAppliedFromServer;

        BattleManager.Instance?.UnregisterPlayer(this);
    }

    [Server]
    private void OnStatsAppliedFromServer()
    {
        currentHp = GetMaxHp();
    }

    [Server]
    public void TakeDamage(float damage, GameObject attacker)
    {
        if (IsDead) return;

        float defense      = charStats != null ? charStats.FinalDefense : 0f;
        float actualDamage = Mathf.Max(1f, damage - defense);
        currentHp          = Mathf.Max(0f, currentHp - actualDamage);

        var attackerStats = attacker?.GetComponent<PlayerStats>();
        if (attackerStats != null)
            BattleManager.Instance?.RecordDamage(attackerStats, actualDamage);

        if (IsDead) RpcOnDeath(attacker);
    }

    [ClientRpc]
    private void RpcOnDeath(GameObject killer)
    {
        if (isServer) BattleManager.Instance?.OnPlayerDead(this);
    }

    private float GetMaxHp() => charStats != null ? charStats.FinalMaxHp : 0f;

    private void OnHpChanged(float _, float newHp)
    {
        hpBar?.UpdateHP(newHp, GetMaxHp());
    }

    [Server]
    public void Respawn() => currentHp = GetMaxHp();
}
