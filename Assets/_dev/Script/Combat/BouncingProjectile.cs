using Mirror;
using UnityEngine;

/// <summary>
/// 벽 반사 투사체 — 반사각으로 벽에 튕김
///
/// Layer 설정 필수:
///   wallLayer    → 벽 오브젝트의 레이어 (Project Settings > Physics Matrix에서
///                  이 투사체 레이어 ↔ 벽 레이어 충돌 활성화)
///   targetLayer  → AttackData.targetLayer (플레이어 레이어)
///
/// 프리팹에 이 컴포넌트만 붙이면 반사 동작
/// </summary>
public class BouncingProjectile : ProjectileBase
{
    [Header("반사")]
    [Tooltip("최대 반사 횟수. 이후 소멸")]
    public int       maxBounces = 3;
    public LayerMask wallLayer;

    [Header("이펙트")]
    public GameObject bounceFxPrefab;

    private int bounceCount = 0;

    protected override void HandleTriggerEnter(Collider other)
    {
        if (IsWall(other))
        {
            Bounce(other);
            return;
        }

        OnHit(other);
        DestroySelf();
    }

    [Server]
    private void Bounce(Collider wallCollider)
    {
        // ClosestPoint로 벽 표면의 가장 가까운 점 → 법선 계산
        Vector3 closest = wallCollider.ClosestPoint(transform.position);
        Vector3 normal  = (transform.position - closest).normalized;
        direction       = Vector3.Reflect(direction, normal).normalized;

        // 같은 벽에 연속 충돌 방지: 반사 후 약간 밀어냄
        transform.position += direction * 0.15f;

        bounceCount++;
        RpcPlayBounceEffect(transform.position);

        if (bounceCount >= maxBounces) DestroySelf();
    }

    private bool IsWall(Collider other)
    {
        return ((1 << other.gameObject.layer) & wallLayer) != 0;
    }

    [ClientRpc]
    private void RpcPlayBounceEffect(Vector3 pos)
    {
        if (bounceFxPrefab == null) return;
        Destroy(Instantiate(bounceFxPrefab, pos, Quaternion.identity), 1f);
    }
}
