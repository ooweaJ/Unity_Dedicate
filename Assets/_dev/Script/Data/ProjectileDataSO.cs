using UnityEngine;

/// <summary>
/// 기본 투사체 수치 정의
///
/// 스폰 흐름:
///   1. ProjectileType → ProjectileRegistry에서 behavior 프리팹 조회
///   2. 해당 프리팹 Instantiate → Init(data) 호출
///   3. visualPrefab → 런타임에 자식으로 부착 (클라이언트 동기화 포함)
///
/// Project 우클릭 → Create → Game/Projectile/Basic
/// </summary>
[CreateAssetMenu(fileName = "NewProjectile", menuName = "Game/Projectile/Basic")]
public class ProjectileDataSO : ScriptableObject
{
    [Header("타입")]
    [Tooltip("어떤 behavior 프리팹을 쓸지 결정 (ProjectileRegistry 참조)")]
    public ProjectileType projectileType;
    [Tooltip("외형만 담당하는 자식 프리팹 — NetworkIdentity 불필요")]
    public GameObject     visualPrefab;

    [Header("이동")]
    public float speed    = 15f;
    public float lifeTime = 3f;

    [Header("발사")]
    public int   count       = 1;
    [Tooltip("전체 퍼짐 각도 (3발·30도 → 좌15°~우15°)")]
    public float spreadAngle = 0f;

    [Header("충돌")]
    public LayerMask hitLayers;

    [Header("상태이상")]
    public StatusEffect onHitEffect;

    [Header("이펙트")]
    public EffectType hitEffect = EffectType.ProjectileHit;
}
