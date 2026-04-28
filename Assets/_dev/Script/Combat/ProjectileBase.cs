using Mirror;
using UnityEngine;

/// <summary>
/// 모든 투사체의 공통 기반
/// OnHit()를 오버라이드해서 폭발/관통 등 다른 행동 구현
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

    protected float      damage;
    protected float      knockback;
    protected GameObject owner;
    protected Vector3    direction;

    public void Init(Vector3 dir, GameObject ownerObj, float dmg, float kb = 0f)
    {
        direction = dir.normalized;
        owner     = ownerObj;
        damage    = dmg;
        knockback = kb;
        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation  = Quaternion.LookRotation(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (owner != null && other.transform.root.gameObject == owner) return;

        OnHit(other);
        DestroySelf();
    }

    protected virtual void OnHit(Collider other)
    {
        var info = new DamageInfo(damage, owner, direction, knockback);
        other.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
        other.transform.root.GetComponent<PlayerAnimationController>()?.RpcPlayHit();
    }

    [Server]
    protected void DestroySelf() => NetworkServer.Destroy(gameObject);
}
