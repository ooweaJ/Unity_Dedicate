using Mirror;
using UnityEngine;

/// <summary>
/// 범위 폭발 투사체 — ProjectileBase.OnHit() 오버라이드
/// 폭발 방향은 각 피격체 → 폭발 중심 방향으로 계산
/// </summary>
public class ExplosiveProjectile : ProjectileBase
{
    [Header("폭발")]
    public float     explosionRadius     = 3f;
    [Tooltip("기본 데미지에 곱하는 폭발 배율")]
    public float     explosionMultiplier = 1.5f;
    public LayerMask explosionTargetLayer;

    [Header("이펙트")]
    public GameObject explosionFxPrefab;

    protected override void OnHit(Collider other)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, explosionTargetLayer);

        foreach (var hit in hits)
        {
            if (owner != null && hit.transform.root.gameObject == owner) continue;

            Vector3 blastDir = (hit.transform.position - transform.position).normalized;
            var info = new DamageInfo(damage * explosionMultiplier, owner, blastDir, knockback);
            hit.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
        }

        RpcExplosionEffect(transform.position);
    }

    [ClientRpc]
    private void RpcExplosionEffect(Vector3 pos)
    {
        if (explosionFxPrefab == null) return;
        Destroy(Instantiate(explosionFxPrefab, pos, Quaternion.identity), 2f);
    }
}
