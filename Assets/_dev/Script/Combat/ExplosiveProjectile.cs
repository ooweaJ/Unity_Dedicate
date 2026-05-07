using Mirror;
using UnityEngine;

/// <summary>
/// 폭발 투사체
///
/// OnImpact:        충돌 즉시 폭발
/// OnMaxDistance:   최대 거리 비행 후 자동 폭발 (경로상 충돌 무시)
/// OnTargetReached: 지정 착탄점 도달 시 폭발 (벽 통과, 수류탄형)
///                  CharacterWeapon.PerformProjectile에서 SetTargetPosition() 호출 필요
/// </summary>
public class ExplosiveProjectile : ProjectileBase
{
    private ExplosionTrigger trigger;
    private float            maxDistance;
    private float            traveledDistance;

    // OnTargetReached 전용 — 스폰 후 SetTargetPosition()으로 설정
    [SyncVar] private Vector3 _targetPos;
    private float             _targetDist;

    private float     explosionRadius;
    private float     explosionInnerRadius;
    private float     explosionMultiplier;
    private LayerMask explosionTargetLayer;
    private EffectType explosionEffect;

    public override void Init(Vector3 dir, GameObject ownerObj, float dmg, ProjectileDataSO data)
    {
        base.Init(dir, ownerObj, dmg, data);

        if (data is ExplosiveProjectileDataSO expData)
        {
            trigger              = expData.trigger;
            maxDistance          = expData.maxDistance;
            explosionRadius      = expData.explosionRadius;
            explosionInnerRadius = expData.explosionInnerRadius;
            explosionMultiplier  = expData.explosionMultiplier;
            explosionTargetLayer = expData.explosionTargetLayer;
            explosionEffect      = expData.explosionEffect;
        }
    }

    /// <summary>
    /// OnTargetReached 전용. NetworkServer.Spawn 호출 전에 서버에서 설정한다.
    /// </summary>
    [Server]
    public void SetTargetPosition(Vector3 worldPos)
    {
        _targetPos = worldPos;

        // Y를 무시한 XZ 거리로 도달 판정 (투사체는 수평 이동)
        Vector3 sp = transform.position;
        _targetDist = Vector3.Distance(
            new Vector3(sp.x,        0f, sp.z),
            new Vector3(worldPos.x,  0f, worldPos.z));
    }

    // ─── 이동 및 도달 판정 ────────────────────────────────────────────────
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 서버 전용 rb.MovePosition

        if (!isServer) return;

        switch (trigger)
        {
            case ExplosionTrigger.OnMaxDistance:
                traveledDistance += speed * Time.fixedDeltaTime;
                if (traveledDistance >= maxDistance)
                {
                    Explode();
                    DestroySelf();
                }
                break;

            case ExplosionTrigger.OnTargetReached:
                if (_targetPos == Vector3.zero) break;
                traveledDistance += speed * Time.fixedDeltaTime;
                if (traveledDistance >= _targetDist)
                {
                    Explode();
                    DestroySelf();
                }
                break;
        }
    }

    // ─── 충돌 처리 ────────────────────────────────────────────────────────
    protected override void HandleTriggerEnter(Collider other)
    {
        // 거리/착탄점 모드는 벽 통과 — hitLayers를 적 레이어만으로 설정해야 함
        if (trigger == ExplosionTrigger.OnMaxDistance ||
            trigger == ExplosionTrigger.OnTargetReached) return;

        Explode();
        DestroySelf();
    }

    // ─── 폭발 판정 ────────────────────────────────────────────────────────
    [Server]
    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, explosionTargetLayer);

        foreach (var hit in hits)
        {
            if (owner != null && hit.transform.root.gameObject == owner) continue;

            float dist     = Vector3.Distance(transform.position, hit.transform.position);
            float range    = Mathf.Max(0.01f, explosionRadius - explosionInnerRadius);
            float falloff  = 1f - Mathf.Clamp01((dist - explosionInnerRadius) / range);
            float finalDmg = damage * explosionMultiplier * falloff;

            Vector3 blastDir = (hit.transform.position - transform.position).normalized;
            var info = new DamageInfo(finalDmg, owner, blastDir, statusEffect);
            hit.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
            hit.transform.root.GetComponent<PlayerAnimationController>()
                ?.RpcPlayHit(hit.transform.position, hitEffect);
        }

        owner?.GetComponent<PlayerBushState>()?.RevealTemporarily();
        RpcPlayExplosionEffect(transform.position, explosionEffect);
    }

    [ClientRpc]
    private void RpcPlayExplosionEffect(Vector3 pos, EffectType effect)
    {
        EffectManager.Instance?.Play(effect, pos);
    }
}
