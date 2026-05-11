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
/// </summary>
public class ProjectileBase : NetworkBehaviour
{
    // 외형 프리팹 인덱스 — Init에서 서버가 설정, 훅이 클라이언트에서 비주얼 부착
    [SyncVar(hook = nameof(OnVisualIndexChanged))]
    private int visualIndex = -1;

    protected float        speed;
    protected float        lifeTime;
    protected LayerMask    hitLayers;
    protected EffectType   hitEffect;
    protected float        damage;
    protected StatusEffect statusEffect;
    protected GameObject   owner;

    protected Rigidbody rb;

    [SyncVar] protected Vector3 direction;

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
        hitLayers    = data != null ? data.hitLayers  : ~0;
        hitEffect    = data != null ? data.hitEffect  : EffectType.ProjectileHit;
        statusEffect = data != null ? data.onHitEffect : StatusEffect.None;
        visualIndex  = ProjectileManager.Instance?.GetVisualIndex(data?.visualPrefab) ?? -1;

        Invoke(nameof(DestroySelf), lifeTime);
    }

    // SyncVar 훅 — 서버/클라이언트 모두 호출됨 (스폰 시 초기값 포함)
    private void OnVisualIndexChanged(int _, int newIndex)
    {
        var visual = ProjectileManager.Instance?.GetVisual(newIndex);
        Debug.Log($"[DBG-VISUAL] index={newIndex}  visual={(visual != null ? visual.name : "NULL")}  isServer={isServer}");
        if (visual == null) return;

        var obj = Instantiate(visual, transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    protected virtual void FixedUpdate()
    {
        // 이동은 서버만 담당 — 클라이언트는 NetworkTransform이 위치를 동기화
        if (!isServer) return;
        if (direction == Vector3.zero) return;
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (other.isTrigger) return;
        if ((hitLayers & (1 << other.gameObject.layer)) == 0) return;
        if (owner != null && other.transform.root.gameObject == owner) return;

        HandleTriggerEnter(other);
    }

    protected virtual void HandleTriggerEnter(Collider other)
    {
        OnHit(other);
        DestroySelf();
    }

    protected virtual void OnHit(Collider other)
    {
        var info = new DamageInfo(damage, owner, direction, statusEffect);
        other.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
        other.transform.root.GetComponent<PlayerAnimationController>()
            ?.RpcPlayHit(transform.position, hitEffect);

        // 공격자가 부쉬 안, 피격자가 부쉬 밖 → 공격자 노출
        var ownerBushState  = owner?.GetComponent<PlayerBushState>();
        if (ownerBushState != null && ownerBushState.inBush)
        {
            var victimBushState = other.transform.root.GetComponent<PlayerBushState>();
            if (victimBushState == null || !victimBushState.inBush)
                ownerBushState.RevealTemporarily();
        }
    }

    [Server]
    protected void DestroySelf() => NetworkServer.Destroy(gameObject);
}
