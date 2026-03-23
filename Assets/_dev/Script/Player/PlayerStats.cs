using Mirror;
using UnityEngine;

/// <summary>
/// HP 관리 + IDamageable 구현
/// CharacterStats.FinalMaxHp 기반으로 HP 설정
/// BattleManager에 등록/해제
/// </summary>
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
        currentHp = GetMaxHp();

        if (charStats != null)
            charStats.OnUpgraded += OnCharacterUpgraded;

        // BattleManager에 등록
        BattleManager.Instance?.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        if (charStats != null)
            charStats.OnUpgraded -= OnCharacterUpgraded;

        BattleManager.Instance?.UnregisterPlayer(this);
    }

    // ─── IDamageable ──────────────────────────────────────────────────
    [Server]
    public void TakeDamage(float damage, GameObject attacker)
    {
        if (IsDead) return;

        float defense      = charStats != null ? charStats.FinalDefense : 0f;
        float actualDamage = Mathf.Max(1f, damage - defense);
        currentHp          = Mathf.Max(0f, currentHp - actualDamage);

        // 데미지 기록 (결과창 표시용)
        var attackerStats = attacker?.GetComponent<PlayerStats>();
        if (attackerStats != null)
            BattleManager.Instance?.RecordDamage(attackerStats, actualDamage);

        if (IsDead)
            RpcOnDeath(attacker);
    }

    [ClientRpc]
    private void RpcOnDeath(GameObject killer)
    {
        if (isServer)
        {
            var killerStats = killer?.GetComponent<PlayerStats>();
            if (killerStats != null)
                BattleManager.Instance?.RecordKill(killerStats, this);

            BattleManager.Instance?.OnPlayerDead(this);
        }
    }

    // ─── 강화 시 HP 비율 유지 ─────────────────────────────────────────
    [Server]
    private void OnCharacterUpgraded(int _)
    {
        float ratio = currentHp / GetMaxHp();
        currentHp   = GetMaxHp() * ratio;
    }

    private float GetMaxHp() =>
        charStats != null ? charStats.FinalMaxHp : 100f;

    // ─── SyncVar Hook → HP바 UI 갱신 ─────────────────────────────────
    private void OnHpChanged(float _, float newHp)
    {
        hpBar?.UpdateHP(newHp, GetMaxHp());
    }

    [Server]
    public void Respawn() => currentHp = GetMaxHp();
}
