using Mirror;
using UnityEngine;

/// <summary>
/// 반사 투사체 — BouncingProjectileDataSO에서 수치 주입
/// 벽 감지는 SphereCast로 처리 (빠른 투사체의 터널링 방지)
/// </summary>
public class BouncingProjectile : ProjectileBase
{
    private int       maxBounces;
    private LayerMask wallLayer;
    private EffectType wallHitEffect;

    private int            bounceCount;
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
        {
            maxBounces    = bounceData.maxBounces;
            wallLayer     = bounceData.wallLayer;
            wallHitEffect = bounceData.wallHitEffect;
        }
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
        if (IsWall(other)) return;
        OnHit(other);
        DestroySelf();
    }

    [Server]
    private void ServerBounce(Vector3 normal)
    {
        direction = Vector3.Reflect(direction, normal).normalized;
        bounceCount++;
        RpcPlayWallHitEffect(transform.position, wallHitEffect);
        if (bounceCount >= maxBounces) DestroySelf();
    }

    private bool IsWall(Collider other) => ((1 << other.gameObject.layer) & wallLayer) != 0;

    [ClientRpc]
    private void RpcPlayWallHitEffect(Vector3 pos, EffectType effect)
    {
        EffectManager.Instance?.Play(effect, pos);
    }
}
