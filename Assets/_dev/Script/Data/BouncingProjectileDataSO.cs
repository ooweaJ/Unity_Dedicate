using UnityEngine;

/// <summary>
/// 반사 투사체 수치 정의
/// wallLayer / wallHitEffectPrefab 은 기반 ProjectileDataSO 에서 설정
/// Project 우클릭 → Create → Game/Projectile/Bouncing
/// </summary>
[CreateAssetMenu(fileName = "NewBouncingProjectile", menuName = "Game/Projectile/Bouncing")]
public class BouncingProjectileDataSO : ProjectileDataSO
{
    [Header("반사")]
    [Tooltip("최대 반사 횟수. 이후 소멸")]
    public int maxBounces = 3;
}
