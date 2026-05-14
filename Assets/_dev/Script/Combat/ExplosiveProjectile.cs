using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ExplosiveProjectile : ProjectileBase
{
    [SyncVar] private ExplosionTrigger trigger;
    private float                       maxDistance;
    private float                       traveledDistance;

    // OnTargetReached 전용 — SyncVar로 클라이언트에 초기값 전달
    [SyncVar] private Vector3 _spawnPos;
    [SyncVar] private Vector3 _targetPos;
    [SyncVar] private bool    _hasTarget;
    [SyncVar] private float   _spawnNetTime;
    [SyncVar] private float   _arcHeight;
    [SyncVar] private float   explosionRadius;

    private float _targetDist;

    private float      explosionInnerRadius;
    private float      explosionMultiplier;
    private LayerMask  explosionTargetLayer;
    private EffectType explosionEffect;

    private GameObject _impactIndicator;

    // 스폰 직후 모든 클라이언트에 RPC로 인디케이터 요청
    // (OnStartClient SyncVar 타이밍에 의존하지 않음)
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_hasTarget && trigger == ExplosionTrigger.OnTargetReached)
            RpcShowImpactIndicator(_targetPos, explosionRadius);
    }

    [ClientRpc]
    private void RpcShowImpactIndicator(Vector3 targetPos, float radius)
    {
        SpawnImpactIndicatorAt(targetPos, radius);
    }

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

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isServer && _hasTarget)
            InitArcLocally();

        // 뒤늦게 접속한 클라이언트 대비 — RPC를 놓쳤을 경우 SyncVar로 생성
        if (_hasTarget && trigger == ExplosionTrigger.OnTargetReached && _impactIndicator == null)
            SpawnImpactIndicatorAt(_targetPos, explosionRadius);
    }

    private void InitArcLocally()
    {
        _targetDist = Vector3.Distance(
            new Vector3(_spawnPos.x, 0f, _spawnPos.z),
            new Vector3(_targetPos.x, 0f, _targetPos.z));

        // 네트워크 지연 보정 — 서버가 이미 이동한 만큼 앞에서 시작
        float elapsed = (float)NetworkTime.time - _spawnNetTime;
        traveledDistance = Mathf.Clamp(elapsed * speed, 0f, _targetDist);
    }

    private void SpawnImpactIndicatorAt(Vector3 targetPos, float radius)
    {
        if (radius <= 0f || _impactIndicator != null) return;

        _impactIndicator = new GameObject("ImpactIndicator");

        const int segments = 48;
        float     y        = targetPos.y + 0.05f;
        var       mat      = new Material(Shader.Find("Sprites/Default"));

        // 채워진 disc
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(_impactIndicator.transform, false);
        fillGo.transform.position   = new Vector3(targetPos.x, y - 0.01f, targetPos.z);
        fillGo.transform.localScale = Vector3.one * (radius * 2f);

        var mf = fillGo.AddComponent<MeshFilter>();
        var mr = fillGo.AddComponent<MeshRenderer>();
        mf.mesh          = BuildDiscMesh(segments);
        mr.material      = new Material(mat) { color = new Color(1f, 0.15f, 0.15f, 0.4f) };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        // 테두리 링
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(_impactIndicator.transform, false);

        var lr = borderGo.AddComponent<LineRenderer>();
        lr.useWorldSpace        = true;
        lr.loop                 = true;
        lr.positionCount        = segments;
        lr.startWidth           = 0.12f;
        lr.endWidth             = 0.12f;
        lr.startColor           = new Color(1f, 0.15f, 0.15f, 1f);
        lr.endColor             = new Color(1f, 0.15f, 0.15f, 1f);
        lr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows       = false;
        lr.generateLightingData = false;
        lr.material             = mat;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            lr.SetPosition(i, new Vector3(
                targetPos.x + Mathf.Cos(angle) * radius,
                y,
                targetPos.z + Mathf.Sin(angle) * radius));
        }
    }

    private static Mesh BuildDiscMesh(int segs)
    {
        var mesh  = new Mesh();
        var verts = new Vector3[segs + 1];
        var tris  = new int[segs * 3];

        verts[0] = Vector3.zero;
        for (int i = 0; i < segs; i++)
        {
            float angle = i * Mathf.PI * 2f / segs;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
            int next = i < segs - 1 ? i + 2 : 1;
            tris[i * 3]     = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = next;
        }

        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void OnDestroy()
    {
        if (_impactIndicator != null)
            Destroy(_impactIndicator);
    }

    [Server]
    public void SetTargetPosition(Vector3 worldPos)
    {
        _spawnPos     = transform.position;
        _targetPos    = worldPos;
        _spawnNetTime = (float)NetworkTime.time;
        _hasTarget    = true;

        _targetDist = Vector3.Distance(
            new Vector3(_spawnPos.x, 0f, _spawnPos.z),
            new Vector3(worldPos.x,  0f, worldPos.z));
    }

    // ─── 이동 ─────────────────────────────────────────────────────────────

    protected override void FixedUpdate()
    {
        if (trigger == ExplosionTrigger.OnTargetReached)
        {
            MoveArc();  // 서버·클라이언트 모두 직접 계산 — NetworkTransform 불필요
            return;
        }

        if (!isServer) return;

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
        if (!_hasTarget || _targetDist == 0f) return;

        traveledDistance += speed * Time.fixedDeltaTime;
        float t = Mathf.Clamp01(traveledDistance / _targetDist);

        Vector3 flatPos = Vector3.Lerp(
            new Vector3(_spawnPos.x, 0f, _spawnPos.z),
            new Vector3(_targetPos.x, 0f, _targetPos.z), t);
        float   arcY    = _arcHeight * 4f * t * (1f - t);
        Vector3 nextPos = new Vector3(flatPos.x, _spawnPos.y + arcY, flatPos.z);

        Vector3 moveDir = nextPos - transform.position;
        if (moveDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDir);

        transform.position = nextPos;

        if (t >= 1f && isServer)
        {
            Explode();
            DestroySelf();
        }
    }

    // ─── 충돌 처리 ────────────────────────────────────────────────────────

    protected override void HandleTriggerEnter(Collider other)
    {
        if (trigger == ExplosionTrigger.OnMaxDistance ||
            trigger == ExplosionTrigger.OnTargetReached) return;

        Explode();
        DestroySelf();
    }

    // ─── 폭발 판정 ────────────────────────────────────────────────────────

    [Server]
    private void Explode()
    {
        Collider[] hits     = Physics.OverlapSphere(transform.position, explosionRadius, explosionTargetLayer);
        var        hitRoots = new HashSet<GameObject>();

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

            Vector3 blastDir = (root.transform.position - transform.position).normalized;
            var     info     = new DamageInfo(finalDmg, owner, blastDir, statusEffect);
            root.GetComponent<IDamageable>()?.TakeDamage(info);
            // 폭발 FX가 메인 이펙트 — 피격 애니메이션만 트리거
            root.GetComponent<PlayerAnimationController>()?.RpcPlayHit(closest, EffectType.None);
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

            Debug.DrawLine(
                pos + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * explosionRadius,
                pos + new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2)) * explosionRadius,
                Color.red, dur);

            if (explosionInnerRadius > 0f)
                Debug.DrawLine(
                    pos + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * explosionInnerRadius,
                    pos + new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2)) * explosionInnerRadius,
                    Color.yellow, dur);
        }
    }
#endif
}
