using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ExplosiveProjectile : ProjectileBase
{
    private ExplosionTrigger trigger;
    private float            maxDistance;
    private float            traveledDistance;

    // OnTargetReached 전용
    [SyncVar] private Vector3 _targetPos;
    private bool              _hasTarget;   // Vector3.zero 착탄점 버그 방지용
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

        Debug.Log($"[Explosive] Init — trigger={trigger}, arcHeight={_arcHeight}, " +
                  $"radius={explosionRadius}, dmg={dmg}, multiplier={explosionMultiplier}");
    }

    [Server]
    public void SetTargetPosition(Vector3 worldPos)
    {
        _spawnPos  = transform.position;
        _targetPos = worldPos;
        _hasTarget = true;

        _targetDist = Vector3.Distance(
            new Vector3(_spawnPos.x, 0f, _spawnPos.z),
            new Vector3(worldPos.x,  0f, worldPos.z));

        Debug.Log($"[DBG-SET] spawn={_spawnPos:F2}  target={worldPos:F2}  dist={_targetDist:F2}  speed={speed}  예상비행={_targetDist/Mathf.Max(speed,0.01f):F2}s");
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
    private bool _dbgFirstArcFrame = true;

    private void MoveArc()
    {
        if (!_hasTarget)
        {
            Debug.LogWarning("[DBG-ARC] _hasTarget=false → 이동 안 함 (SetTargetPosition 미호출)");
            return;
        }

        traveledDistance += speed * Time.fixedDeltaTime;
        float t = _targetDist > 0f ? Mathf.Clamp01(traveledDistance / _targetDist) : 1f;

        Vector3 flatPos = Vector3.Lerp(
            new Vector3(_spawnPos.x, 0f, _spawnPos.z),
            new Vector3(_targetPos.x, 0f, _targetPos.z), t);
        float   arcY    = _arcHeight * 4f * t * (1f - t);
        Vector3 nextPos = new Vector3(flatPos.x, _spawnPos.y + arcY, flatPos.z);

        if (_dbgFirstArcFrame)
        {
            _dbgFirstArcFrame = false;
            Debug.Log($"[DBG-ARC 1프레임] t={t:F3}  traveled={traveledDistance:F3}  targetDist={_targetDist:F2}  " +
                      $"speed={speed}  rb.pos={rb.position:F2}  nextPos={nextPos:F2}");
        }

        Vector3 moveDir = nextPos - rb.position;
        if (moveDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDir);

        if (t >= 1f)
        {
            // rb.position은 FixedUpdate 내 transform과 동기화 안 됨 → transform.position 직접 설정
            transform.position = nextPos;
            Debug.Log($"[DBG-ARC 도착] nextPos={nextPos:F2}  tf.pos(설정 후)={transform.position:F2}");
            Explode();
            DestroySelf();
        }
        else
        {
            rb.MovePosition(nextPos);
        }
    }

    // ─── 충돌 처리 ────────────────────────────────────────────────────────

    protected override void HandleTriggerEnter(Collider other)
    {
        Debug.Log($"[DBG-HIT] trigger={trigger}  other={other.name}  layer={LayerMask.LayerToName(other.gameObject.layer)}  tf.pos={transform.position:F2}");
        if (trigger == ExplosionTrigger.OnMaxDistance ||
            trigger == ExplosionTrigger.OnTargetReached) return;

        Explode();
        DestroySelf();
    }

    // ─── 폭발 판정 ────────────────────────────────────────────────────────

    [Server]
    private void Explode()
    {
        Debug.Log($"[DBG-EXPLODE] tf.pos={transform.position:F2}  rb.pos={rb.position:F2}  " +
                  $"radius={explosionRadius}  layer={explosionTargetLayer.value}  dmg={damage}x{explosionMultiplier}");

        Collider[] hits     = Physics.OverlapSphere(transform.position, explosionRadius, explosionTargetLayer);
        var        hitRoots = new HashSet<GameObject>();

        Debug.Log($"[Explosive] OverlapSphere hit {hits.Length} colliders");

        foreach (var hit in hits)
        {
            var root = hit.transform.root.gameObject;
            if (owner != null && root == owner) continue;
            if (!hitRoots.Add(root)) continue;

            Vector3 closest  = hit.ClosestPoint(transform.position);
            float   dist     = Vector3.Distance(transform.position, closest);
            float   range    = Mathf.Max(0.01f, explosionRadius - explosionInnerRadius);
            float   falloff  = 1f - Mathf.Clamp01((dist - explosionInnerRadius) / range);
            float   finalDmg = damage * explosionMultiplier * falloff;

            Debug.Log($"[Explosive] Hit '{root.name}' — dist={dist:F2}, falloff={falloff:F2}, finalDmg={finalDmg:F1}");

            Vector3 blastDir = (root.transform.position - transform.position).normalized;
            var     info     = new DamageInfo(finalDmg, owner, blastDir, statusEffect);
            root.GetComponent<IDamageable>()?.TakeDamage(info);
            root.GetComponent<PlayerAnimationController>()?.RpcPlayHit(closest, hitEffect);
        }

        owner?.GetComponent<PlayerBushState>()?.RevealTemporarily();
        RpcPlayExplosionEffect(transform.position, explosionEffect);

#if UNITY_EDITOR
        DrawExplosionGizmo();
#endif
    }

    [ClientRpc]
    private void RpcPlayExplosionEffect(Vector3 pos, EffectType effect)
    {
        EffectManager.Instance?.Play(effect, pos);
    }

    // ─── 에디터 디버그 시각화 ─────────────────────────────────────────────

#if UNITY_EDITOR
    private void DrawExplosionGizmo()
    {
        const int   seg = 48;
        const float dur = 3f;
        Vector3     pos = transform.position;

        for (int i = 0; i < seg; i++)
        {
            float a1 = i       * Mathf.PI * 2f / seg;
            float a2 = (i + 1) * Mathf.PI * 2f / seg;

            // 빨강 = 폭발 반경 (XZ 수평)
            Debug.DrawLine(
                pos + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * explosionRadius,
                pos + new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2)) * explosionRadius,
                Color.red, dur);

            // 노랑 = 풀 데미지 내부 반경
            if (explosionInnerRadius > 0f)
                Debug.DrawLine(
                    pos + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * explosionInnerRadius,
                    pos + new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2)) * explosionInnerRadius,
                    Color.yellow, dur);
        }
    }
#endif
}
