using Mirror;
using UnityEngine;

/// <summary>
/// 벽 반사 투사체 — 반사각으로 벽에 튕김
///
/// 벽 감지는 OnTriggerEnter 대신 FixedUpdate SphereCast로 처리
/// → 빠른 투사체가 얇은 벽을 한 프레임에 통과(터널링)하는 문제 방지
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

    private int            bounceCount = 0;
    private SphereCollider col;

    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<SphereCollider>();
    }

    // ─── 이동 + 벽 감지 (터널링 방지) ────────────────────────────────────
    protected override void FixedUpdate()
    {
        if (direction == Vector3.zero) return;

        // 클라이언트는 순수 이동만 (벽 판정은 서버 전담)
        if (!isServer) { base.FixedUpdate(); return; }

        float moveDist = speed * Time.fixedDeltaTime;
        float radius   = col != null ? col.radius * 0.9f : 0.1f;

        // 이동 경로 미리 sweep → 벽에 닿기 전에 감지
        if (Physics.SphereCast(rb.position, radius, direction, out RaycastHit hit,
                moveDist + radius, wallLayer, QueryTriggerInteraction.Collide))
        {
            // 벽 표면 바로 앞까지만 이동
            float safeMove = Mathf.Max(0f, hit.distance - radius);
            rb.MovePosition(rb.position + direction * safeMove);
            transform.rotation = Quaternion.LookRotation(direction);
            ServerBounce(hit.normal);
            return;
        }

        base.FixedUpdate();
    }

    // ─── 플레이어 피격은 OnTriggerEnter 유지 ─────────────────────────────
    protected override void HandleTriggerEnter(Collider other)
    {
        if (IsWall(other)) return;  // 벽은 SphereCast가 처리
        OnHit(other);
        DestroySelf();
    }

    [Server]
    private void ServerBounce(Vector3 normal)
    {
        direction = Vector3.Reflect(direction, normal).normalized;
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
