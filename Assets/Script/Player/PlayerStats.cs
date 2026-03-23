using Mirror;
using UnityEngine;

/// <summary>
/// HP 관리 + IDamageable 구현
/// maxHp는 CharacterStats.FinalMaxHp 참조 — 강화 시 자동 반영
/// </summary>
public class PlayerStats : NetworkBehaviour, IDamageable
{
    [SyncVar(hook = nameof(OnHpChanged))]
    private float currentHp;

    private CharacterStats charStats;
    private PlayerHPBar    hpBar;

    // IDamageable
    public bool IsDead => currentHp <= 0f;

    private void Awake()
    {
        charStats = GetComponent<CharacterStats>();
        hpBar     = GetComponentInChildren<PlayerHPBar>();
    }

    public override void OnStartServer()
    {
        // 서버 시작 시 CharacterStats 기반으로 초기 HP 설정
        currentHp = charStats != null ? charStats.FinalMaxHp : 100f;

        // 강화 시 MaxHP가 올라가면 현재 HP도 비율 유지
        if (charStats != null)
            charStats.OnUpgraded += OnCharacterUpgraded;
    }

    public override void OnStopServer()
    {
        if (charStats != null)
            charStats.OnUpgraded -= OnCharacterUpgraded;
    }
    public float CurrentHpRatio => charStats != null
    ? currentHp / charStats.FinalMaxHp
    : currentHp / 100f;

    // ─── IDamageable 구현 ────────────────────────────────────────────────
    [Server]
    public void TakeDamage(float damage, GameObject attacker)
    {
        if (IsDead) return;
        float defense = charStats != null ? charStats.FinalDefense : 0f;
        float actualDamage = Mathf.Max(1f, damage - defense);
        currentHp = Mathf.Max(0f, currentHp - actualDamage);

        // 데미지 기록 → 결과창에 표시
        var attackerStats = attacker?.GetComponent<PlayerStats>();
        if (attackerStats != null)
            BattleManager.Instance?.RecordDamage(attackerStats, actualDamage);

        if (IsDead) RpcOnDeath(attacker);
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

    // ─── 강화 시 HP 비율 유지 ─────────────────────────────────────────────
    [Server]
    private void OnCharacterUpgraded(int _)
    {
        float ratio   = currentHp / GetMaxHp();   // 강화 전 HP 비율 저장
        currentHp     = GetMaxHp() * ratio;        // 새 MaxHp에 비율 적용
    }

    private float GetMaxHp() =>
        charStats != null ? charStats.FinalMaxHp : 100f;

    // ─── SyncVar Hook : HP바 UI 갱신 ────────────────────────────────────
    private void OnHpChanged(float _, float newHp)
    {
        hpBar?.UpdateHP(newHp, GetMaxHp());
    }

    // ─── 부활 / 리스폰 (서버 전용) ───────────────────────────────────────
    [Server]
    public void Respawn()
    {
        currentHp = GetMaxHp();
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        Debug.Log($"[STATS] {gameObject.name} 사망");
        // 사망 연출, UI 처리 등 추가
    }
}
