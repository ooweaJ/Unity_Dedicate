using Mirror;
using UnityEngine;

/// <summary>
/// 폭발 투사체
///
/// OnImpact:        충돌 즉시 폭발
/// OnMaxDistance:   최대 거리 직선 비행 후 자동 폭발 (경로상 충돌 무시)
/// OnTargetReached: 지정 착탄점까지 포물선(수류탄) 비행 후 폭발 (벽 통과)
///                  CharacterWeapon.PerformProjectile에서 SetTargetPosition() 호출 필요
/// </summary>
public class ExplosiveProjectile : ProjectileBase
{
    private ExplosionTrigger trigger;
    private float            maxDistance;
    private float            traveledDistance;

    // OnTargetReached 전용
    [SyncVar] private Vector3 _targetPos;
    private Vector3           _spawnPos;
    private float             _targetDist;
    private float             _arcHeight;

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
            _arcHeight           = expData.arcHeight;
        }
    }

    /// <summary>
    /// OnTargetReached 전용. NetworkServer.Spawn 호출 전에 서버에서 설정한다.
    /// </summary>
    [Server]
    public void SetTargetPosition(Vector3 worldPos)
    {
        _spawnPos  = transform.position;
        _targetPos = worldPos;

        _targetDist = Vector3.Distance(
            new Vector3(_spawnPos.x, 0f, _spawnPos.z),
            new Vector3(worldPos.x,  0f, worldPos.z));
    }

    // ─── 이동 ─────────────────────────────────────────────────────────────
    protected override void FixedUpdate()
    {
        if (!isServer) return;

        if (trigger == ExplosionTrigger.OnTargetReached)
        {
            MoveArc();
            return;
        }

        base.FixedUpdate();

        if (trigger == ExplosionTrigger.OnMaxDistance)
        {
            traveledDistance += speed * Time.fixedDeltaTime;
            if (traveledDistance >= maxDistance)
            {
                Explode();
                DestroySelf();
            }
        }
    }

    // 포물선 이동 — 수평 속도 일정, Y는 4t(1-t) 포물선
    private void MoveArc()
    {
        if (_targetPos == Vector3.zero) return;

        traveledDistance += speed * Time.fixedDeltaTime;
        float t = _targetDist > 0f ? Mathf.Clamp01(traveledDistance / _targetDist) : 1f;

        Vector3 flatPos = Vector3.Lerp(
            new Vector3(_spawnPos.x, 0f, _spawnPos.z),
            new Vector3(_targetPos.x, 0f, _targetPos.z), t);
        float   arcY    = _arcHeight * 4f * t * (1f - t);
        Vector3 nextPos = new Vector3(flatPos.x, _spawnPos.y + arcY, flatPos.z);

        Vector3 moveDir = nextPos - rb.position;
        if (moveDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDir);
        rb.MovePosition(nextPos);

        if (t >= 1f)
        {
            Explode();
            DestroySelf();
        }
    }

    // ─── 충돌 처리 ────────────────────────────────────────────────────────
    protected override void HandleTriggerEnter(Collider other)
    {
        // 거리/착탄점 모드는 충돌 무시 (벽 통과)
        if (trigger == ExplosionTrigger.OnMaxDistance ||
            trigger == ExplosionTrigger.OnTargetReached) return;

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

            float dist     = Vector3.Distance(transform.position, hit.transform.position);
            float range    = Mathf.Max(0.01f, explosionRadius - explosionInnerRadius);
            float falloff  = 1f - Mathf.Clamp01((dist - explosionInnerRadius) / range);
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
