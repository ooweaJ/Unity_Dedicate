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

        // 팀원 데미지 무시
        var attackerTeam = info.Attacker?.GetComponent<BattleNetworkPlayer>()?.teamId;
        var myTeam       = GetComponent<BattleNetworkPlayer>()?.teamId;
        if (attackerTeam.HasValue && myTeam.HasValue && attackerTeam == myTeam) return;

        float defense      = charStats != null ? charStats.FinalDefense : 0f;
        float actualDamage = Mathf.Max(1f, info.Amount - defense);
        currentHp          = Mathf.Max(0f, currentHp - actualDamage);

        ApplyStatusEffect(info.Effect, info.Direction, info.Attacker);

        // 공격자 반대 방향으로 1f 오프셋 (캐릭터를 가리지 않게)
        Vector3 awayDir = Vector3.zero;
        if (info.Direction != Vector3.zero)
        {
            awayDir = new Vector3(info.Direction.x, 0f, info.Direction.z).normalized;
        }
        else if (info.Attacker != null)
        {
            Vector3 toVictim = transform.position - info.Attacker.transform.position;
            toVictim.y = 0f;
            if (toVictim.sqrMagnitude > 0.01f)
                awayDir = toVictim.normalized;
        }
        RpcShowDamagePopup(actualDamage, transform.position + awayDir * 1f + Vector3.up * 1.5f);

        var attackerStats = info.Attacker?.GetComponent<PlayerStats>();
        if (attackerStats != null)
            BattleManager.Instance?.RecordDamage(attackerStats, actualDamage);

        if (IsDead)
        {
            // 서버: 콜라이더·물리 즉시 정지 (사후 판정 차단, 중력 낙하 방지)
            foreach (var col in GetComponentsInChildren<Collider>())
                if (!col.isTrigger) col.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }

            BattleManager.Instance?.RecordKill(attackerStats, this);
            RpcOnDeath(info.Attacker);
            BattleManager.Instance?.OnPlayerDead(this);
        }
    }

    [Server]
    private void ApplyStatusEffect(StatusEffect effect, Vector3 direction, GameObject source)
    {
        if (effect.IsNone) return;
        GetComponent<StatusEffectHandler>()?.Apply(effect, direction, source);
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
        // 사망 FX
        var data = GetComponent<CharacterSpawner>()
            ?.GetCharacterData()?.deathEffect ?? EffectType.Death;
        EffectManager.Instance?.Play(data, transform.position);

        // 외형 + HP바 + 팀 인디케이터 숨김
        GetComponent<CharacterSpawner>()?.HideVisual();
        GetComponent<PlayerHPBar>()?.Hide();
        GetComponent<TeamIndicator>()?.Hide();

        // 클라이언트 콜라이더·물리 정지
        foreach (var col in GetComponentsInChildren<Collider>())
            if (!col.isTrigger) col.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }

        // 로컬 플레이어 사망 → 살아있는 팀원으로 카메라 전환
        if (isLocalPlayer)
        {
            var teammate = FindAliveTeammate();
            if (teammate != null)
            {
                var cam = FindFirstObjectByType<PlayerCameraFollow>();
                if (cam != null) cam.target = teammate;
            }
        }
    }

    private Transform FindAliveTeammate()
    {
        int myTeamId = GetComponent<BattleNetworkPlayer>()?.teamId ?? -1;
        if (myTeamId < 0) return null;

        foreach (var ps in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            if (ps == this || ps.IsDead) continue;
            if (ps.GetComponent<BattleNetworkPlayer>()?.teamId == myTeamId)
                return ps.transform;
        }
        return null;
    }

    [Server]
    public void Respawn() => currentHp = GetMaxHp();

    private float GetMaxHp() => charStats != null ? charStats.FinalMaxHp : 0f;

    public override void OnStartClient()
    {
        if (charStats != null)
            charStats.OnStatsApplied += OnStatsAppliedFromClient;
    }

    public override void OnStopClient()
    {
        if (charStats != null)
            charStats.OnStatsApplied -= OnStatsAppliedFromClient;
    }

    private void OnStatsAppliedFromClient()
    {
        hpBar?.UpdateHP(currentHp > 0f ? currentHp : GetMaxHp(), GetMaxHp());
    }

    private void OnHpChanged(float _, float newHp)
    {
        hpBar?.UpdateHP(newHp, GetMaxHp());
    }
}
