using Mirror;
using UnityEngine;

/// <summary>
/// 폭발 투사체
///
/// OnImpact:      충돌 즉시 폭발
/// OnMaxDistance: 최대 거리 비행 후 자동 폭발 (경로상 충돌 무시)
/// </summary>
public class ExplosiveProjectile : ProjectileBase
{
    private ExplosionTrigger trigger;
    private float            maxDistance;
    private float            traveledDistance;

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

    // ─── 거리 추적 (OnMaxDistance 전용) ──────────────────────────────────
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!isServer || trigger != ExplosionTrigger.OnMaxDistance) return;

        traveledDistance += speed * Time.fixedDeltaTime;
        if (traveledDistance >= maxDistance)
        {
            Explode();
            DestroySelf();
        }
    }

    // ─── 충돌 처리 ────────────────────────────────────────────────────────
    protected override void HandleTriggerEnter(Collider other)
    {
        if (trigger == ExplosionTrigger.OnMaxDistance) return; // 거리 폭발 모드는 충돌 무시

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

            float dist    = Vector3.Distance(transform.position, hit.transform.position);
            float range   = Mathf.Max(0.01f, explosionRadius - explosionInnerRadius);
            float falloff = 1f - Mathf.Clamp01((dist - explosionInnerRadius) / range);
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
