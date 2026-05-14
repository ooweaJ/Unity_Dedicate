using Mirror;
using UnityEngine;

/// <summary>
/// 반사 투사체 — BouncingProjectileDataSO에서 수치 주입
/// wallLayer / wallHitEffectPrefab 은 기반 ProjectileDataSO(→ProjectileBase)에서 처리
/// 벽 감지는 SphereCast로 처리 (빠른 투사체의 터널링 방지)
/// </summary>
public class BouncingProjectile : ProjectileBase
{
    private int maxBounces;
    private int bounceCount;

    private SphereCollider col;

    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<SphereCollider>();
    }

    public override void Init(Vector3 dir, GameObject ownerObj, float dmg, ProjectileDataSO data)
    {
        base.Init(dir, ownerObj, dmg, data);

        if (data is BouncingProjectileDataSO bounceData)
            maxBounces = bounceData.maxBounces;
    }

    // ─── 이동 + 벽 감지 (터널링 방지) ────────────────────────────────────
    protected override void FixedUpdate()
    {
        if (direction == Vector3.zero) return;
        if (!isServer) { base.FixedUpdate(); return; }

        float moveDist = speed * Time.fixedDeltaTime;
        float radius   = col != null ? col.radius * 0.9f : 0.1f;

        if (Physics.SphereCast(rb.position, radius, direction, out RaycastHit hit,
                moveDist + radius, wallLayer, QueryTriggerInteraction.Collide))
        {
            float safeMove = Mathf.Max(0f, hit.distance - radius);
            rb.MovePosition(rb.position + direction * safeMove);
            transform.rotation = Quaternion.LookRotation(direction);
            ServerBounce(hit.normal);
            return;
        }

        base.FixedUpdate();
    }

    // ─── 플레이어 피격 ─────────────────────────────────────────────────
    protected override void HandleTriggerEnter(Collider other)
    {
        if (IsWall(other)) return; // 벽은 FixedUpdate SphereCast가 처리
        OnHit(other);
        DestroySelf();
    }

    [Server]
    private void ServerBounce(Vector3 normal)
    {
        direction = Vector3.Reflect(direction, normal).normalized;
        bounceCount++;
        RpcPlayFX(_wallHitFxIndex, transform.position); // 기반 클래스 RPC 재사용
        if (bounceCount >= maxBounces) DestroySelf();
    }
}
