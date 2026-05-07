using UnityEngine;

public enum ExplosionTrigger
{
    OnImpact,        // 벽/플레이어 충돌 즉시 폭발
    OnMaxDistance,   // 최대 거리 비행 후 자동 폭발 (충돌 무시)
    OnTargetReached, // 지정 착탄점 도달 시 폭발 (벽 통과, 수류탄형)
}

/// <summary>
/// 폭발 투사체 수치 정의
/// Project 우클릭 → Create → Game/Projectile/Explosive
/// </summary>
[CreateAssetMenu(fileName = "NewExplosiveProjectile", menuName = "Game/Projectile/Explosive")]
public class ExplosiveProjectileDataSO : ProjectileDataSO
{
    [Header("폭발 트리거")]
    public ExplosionTrigger trigger     = ExplosionTrigger.OnImpact;
    [Tooltip("OnMaxDistance 전용 — 이 거리에 도달하면 자동 폭발")]
    public float            maxDistance = 10f;

    [Header("폭발 범위")]
    public float     explosionRadius      = 3f;
    [Tooltip("이 반경 안은 풀 데미지 — 밖부터 explosionRadius까지 선형 감쇠")]
    public float     explosionInnerRadius = 0f;
    [Tooltip("기본 데미지에 곱하는 폭발 배율")]
    public float     explosionMultiplier  = 1.5f;
    public LayerMask explosionTargetLayer;

    [Header("폭발 이펙트")]
    public EffectType explosionEffect = EffectType.Explosion;
}
