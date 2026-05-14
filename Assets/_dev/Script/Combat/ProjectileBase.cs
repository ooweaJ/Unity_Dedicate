using Mirror;
using UnityEngine;

/// <summary>
/// 모든 투사체의 공통 기반 — 수치와 외형 모두 ProjectileDataSO에서 주입
///
/// behavior 프리팹 구조 (NetworkIdentity 필수, Registered Spawnable Prefabs 등록 필수):
///   ProjectileBehavior
///   ├── Rigidbody        (isKinematic ON, useGravity OFF)
///   ├── SphereCollider   (isTrigger ON)
///   └── ProjectileBase 또는 자식 컴포넌트
///
/// 외형 프리팹은 Init 시점에 자식으로 동적 부착 → visualIndex SyncVar로 클라이언트 동기화
/// FX 프리팹 인덱스(_hitFxIndex / _wallHitFxIndex)도 SyncVar — ProjectileManager.fxPrefabs[] 참조
/// </summary>
public class ProjectileBase : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnVisualIndexChanged))]
    private int visualIndex = -1;

    // fxPrefabs[] 인덱스 — -1이면 FX 없음
    [SyncVar] protected int _hitFxIndex     = -1;
    [SyncVar] protected int _wallHitFxIndex = -1;

    [SyncVar] protected float  speed;
    [SyncVar] protected Vector3 direction;

    protected float        lifeTime;
    protected LayerMask    hitLayers;
    protected LayerMask    wallLayer;
    protected float        damage;
    protected StatusEffect statusEffect;
    protected GameObject   owner;

    protected Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public virtual void Init(Vector3 dir, GameObject ownerObj, float dmg, ProjectileDataSO data)
    {
        direction    = dir.normalized;
        owner        = ownerObj;
        damage       = dmg;
        speed        = data != null ? data.speed      : 15f;
        lifeTime     = data != null ? data.lifeTime   : 3f;
        wallLayer    = data != null ? data.wallLayer  : 0;
        // 벽 레이어도 OnTriggerEnter가 잡을 수 있도록 hitLayers에 포함
        hitLayers    = data != null ? (data.hitLayers | data.wallLayer) : ~0;
        statusEffect = data != null ? data.onHitEffect : StatusEffect.None;

        visualIndex      = ProjectileManager.Instance?.GetVisualIndex(data?.visualPrefab)          ?? -1;
        _hitFxIndex      = ProjectileManager.Instance?.GetFxIndex(data?.hitEffectPrefab)           ?? -1;
        _wallHitFxIndex  = ProjectileManager.Instance?.GetFxIndex(data?.wallHitEffectPrefab)       ?? -1;

        Invoke(nameof(DestroySelf), lifeTime);
    }

    // ─── 비주얼 부착 (클라이언트) ─────────────────────────────────────────
    private void OnVisualIndexChanged(int _, int newIndex)
    {
        var visual = ProjectileManager.Instance?.GetVisual(newIndex);
        if (visual == null) return;

        var obj = Instantiate(visual, transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    // ─── 이동 ──────────────────────────────────────────────────────────────
    protected virtual void FixedUpdate()
    {
        if (!isServer) return;
        if (direction == Vector3.zero) return;
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.LookRotation(direction);
    }

    // ─── 충돌 감지 ─────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (other.isTrigger) return;
        if ((hitLayers & (1 << other.gameObject.layer)) == 0) return;
        if (owner != null && other.transform.root.gameObject == owner) return;

        // 팀원 통과
        var ownerTeam  = owner?.GetComponent<BattleNetworkPlayer>()?.teamId;
        var targetTeam = other.transform.root.GetComponent<BattleNetworkPlayer>()?.teamId;
        if (ownerTeam.HasValue && targetTeam.HasValue && ownerTeam == targetTeam) return;

        HandleTriggerEnter(other);
    }

    protected virtual void HandleTriggerEnter(Collider other)
    {
        if (IsWall(other))
        {
            RpcPlayFX(_wallHitFxIndex, transform.position);
            DestroySelf();
            return;
        }

        OnHit(other);
        DestroySelf();
    }

    // ─── 피격 처리 ─────────────────────────────────────────────────────────
    protected virtual void OnHit(Collider other)
    {
        var info = new DamageInfo(damage, owner, direction, statusEffect);
        other.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);

        // 피격 애니메이션 트리거 (FX는 RpcPlayFX가 담당하므로 EffectType.None)
        other.transform.root.GetComponent<PlayerAnimationController>()
            ?.RpcPlayHit(transform.position, EffectType.None);

        RpcPlayFX(_hitFxIndex, transform.position);

        // 부쉬: 공격자가 부쉬 안, 피격자가 부쉬 밖 → 공격자 노출
        var ownerBushState = owner?.GetComponent<PlayerBushState>();
        if (ownerBushState != null && ownerBushState.inBush)
        {
            var victimBushState = other.transform.root.GetComponent<PlayerBushState>();
            if (victimBushState == null || !victimBushState.inBush)
                ownerBushState.RevealTemporarily();
        }
    }

    // ─── FX RPC ────────────────────────────────────────────────────────────
    [ClientRpc]
    protected void RpcPlayFX(int fxIndex, Vector3 pos)
    {
        var prefab = ProjectileManager.Instance?.GetFxPrefab(fxIndex);
        if (prefab != null)
            Destroy(Instantiate(prefab, pos, Quaternion.identity), 3f);
    }

    // ─── 유틸 ──────────────────────────────────────────────────────────────
    protected bool IsWall(Collider other) =>
        wallLayer != 0 && (wallLayer & (1 << other.gameObject.layer)) != 0;

    [Server]
    protected void DestroySelf() => NetworkServer.Destroy(gameObject);
}
