using Mirror;
using UnityEngine;

/// <summary>
/// 모든 투사체의 공통 기반
/// OnHit()를 오버라이드해서 폭발/관통 등 다른 행동 구현
///
/// 프리팹 구조:
/// Projectile
/// ├── NetworkIdentity       ← 필수 (NetworkServer.Spawn 동기화)
/// ├── Rigidbody             ← useGravity: OFF, isKinematic: ON
/// ├── SphereCollider        ← isTrigger: ON
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
    protected GameObject owner;
    private   Vector3    direction;

    /// <summary>
    /// 서버에서 Spawn 직후 CharacterWeapon.PerformProjectile()이 호출
    /// </summary>
    public void Init(Vector3 dir, GameObject ownerObj, float dmg)
    {
        direction = dir.normalized;
        owner     = ownerObj;
        damage    = dmg;
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

        OnHit(other);   // 자식 클래스에서 오버라이드
        DestroySelf();
    }

    /// <summary>
    /// 기본 동작: 단순 데미지 + 피격 애니
    /// ExplosiveProjectile 등에서 오버라이드해서 다른 행동 추가
    /// </summary>
    protected virtual void OnHit(Collider other)
    {
        other.transform.root.GetComponent<IDamageable>()
            ?.TakeDamage(damage, owner);

        other.transform.root.GetComponent<PlayerAnimationController>()
            ?.RpcPlayHit();
    }

    [Server]
    protected void DestroySelf() => NetworkServer.Destroy(gameObject);
}
