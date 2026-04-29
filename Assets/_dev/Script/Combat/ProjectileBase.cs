using Mirror;
using UnityEngine;

/// <summary>
/// 모든 투사체의 공통 기반
/// HandleTriggerEnter()를 오버라이드해서 반사/관통 등 다른 행동 구현
///
/// 프리팹 구조:
/// Projectile
/// ├── NetworkIdentity  ← 필수 (NetworkServer.Spawn 동기화)
/// ├── Rigidbody        ← useGravity: OFF, isKinematic: ON
/// ├── SphereCollider   ← isTrigger: ON
/// └── ProjectileBase (or 자식 클래스)
///
/// NetworkManager → Registered Spawnable Prefabs 에 등록 필수
/// </summary>
public class ProjectileBase : NetworkBehaviour
{
    [Header("이동")]
    public float speed    = 15f;
    public float lifeTime = 3f;

    [Header("이펙트")]
    public EffectType hitEffect = EffectType.ProjectileHit;

    protected float      damage;
    protected float      knockback;
    protected GameObject owner;

    protected Rigidbody rb;

    // 서버에서 설정 → 스폰 메시지에 포함되어 클라이언트에 자동 전달
    // NetworkTransform 없이 클라이언트가 직접 이동 계산 → 부드러운 움직임
    [SyncVar] protected Vector3 direction;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(Vector3 dir, GameObject ownerObj, float dmg, float kb = 0f)
    {
        direction = dir.normalized;
        owner     = ownerObj;
        damage    = dmg;
        knockback = kb;
        Invoke(nameof(DestroySelf), lifeTime);
    }

    protected virtual void FixedUpdate()
    {
        if (direction == Vector3.zero) return;
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (owner != null && other.transform.root.gameObject == owner) return;

        HandleTriggerEnter(other);
    }

    /// <summary>
    /// 충돌 처리 — 자식 클래스에서 오버라이드 가능
    /// 기본: 데미지 → 소멸
    /// </summary>
    protected virtual void HandleTriggerEnter(Collider other)
    {
        OnHit(other);
        DestroySelf();
    }

    protected virtual void OnHit(Collider other)
    {
        var info = new DamageInfo(damage, owner, direction, knockback);
        other.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
        other.transform.root.GetComponent<PlayerAnimationController>()
            ?.RpcPlayHit(transform.position, hitEffect);
    }

    [Server]
    protected void DestroySelf() => NetworkServer.Destroy(gameObject);
}
