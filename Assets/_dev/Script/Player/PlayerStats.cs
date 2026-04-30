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
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead) return;

        float defense      = charStats != null ? charStats.FinalDefense : 0f;
        float actualDamage = Mathf.Max(1f, info.Amount - defense);
        currentHp          = Mathf.Max(0f, currentHp - actualDamage);

        if (info.Knockback > 0f)
            GetComponent<Rigidbody>()?.AddForce(info.Direction * info.Knockback, ForceMode.Impulse);

        RpcShowDamagePopup(actualDamage, transform.position + Vector3.up * 1.5f);

        var attackerStats = info.Attacker?.GetComponent<PlayerStats>();
        if (attackerStats != null)
            BattleManager.Instance?.RecordDamage(attackerStats, actualDamage);

        if (IsDead)
        {
            // 킬 기록
            BattleManager.Instance?.RecordKill(attackerStats, this);

            // 시각 효과는 ClientRpc, 게임 판정은 직접 서버에서
            RpcOnDeath(info.Attacker);
            BattleManager.Instance?.OnPlayerDead(this);
        }
    }

    [ClientRpc]
    private void RpcShowDamagePopup(float amount, Vector3 worldPos)
    {
        DamagePopupManager.Instance?.Show(amount, worldPos);
    }

    // 클라이언트 전체에 죽음 시각 효과 전달용 (판정 로직 없음)
    [ClientRpc]
    private void RpcOnDeath(GameObject killer)
    {
        var data = GetComponent<CharacterSpawner>()
            ?.GetCharacterData()?.deathEffect ?? EffectType.Death;
        EffectManager.Instance?.Play(data, transform.position);
    }

    [Server]
    public void Respawn() => currentHp = GetMaxHp();

    private float GetMaxHp() => charStats != null ? charStats.FinalMaxHp : 0f;

    private void OnHpChanged(float _, float newHp)
    {
        hpBar?.UpdateHP(newHp, GetMaxHp());
    }
}
