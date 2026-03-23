using Mirror;
using UnityEngine;

/// <summary>
/// 대포알, 폭탄 등 범위 폭발 투사체
/// ProjectileBase.OnHit()를 오버라이드
///
/// 프리팹에 이 컴포넌트만 붙이면 폭발 동작
/// 일반 투사체는 ProjectileBase 그대로 사용
/// </summary>
public class ExplosiveProjectile : ProjectileBase
{
    [Header("폭발")]
    public float     explosionRadius = 3f;
    [Tooltip("기본 데미지에 곱하는 폭발 배율")]
    public float     explosionMultiplier = 1.5f;
    public LayerMask explosionTargetLayer;

    [Header("이펙트")]
    public GameObject explosionFxPrefab;

    protected override void OnHit(Collider other)
    {
        // 폭발 범위 내 모든 대상 피격
        Collider[] hits = Physics.OverlapSphere(
            transform.position, explosionRadius, explosionTargetLayer);

        foreach (var hit in hits)
        {
            if (owner != null && hit.transform.root.gameObject == owner) continue;

            hit.transform.root.GetComponent<IDamageable>()
                ?.TakeDamage(damage * explosionMultiplier, owner);
        }

        // 폭발 이펙트 — 모든 클라이언트
        RpcExplosionEffect(transform.position);
    }

    [ClientRpc]
    private void RpcExplosionEffect(Vector3 pos)
    {
        if (explosionFxPrefab == null) return;
        Destroy(Instantiate(explosionFxPrefab, pos, Quaternion.identity), 2f);
    }
}
