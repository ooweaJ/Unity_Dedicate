using Mirror;
using UnityEngine;

/// <summary>
/// 투사체 프리팹에 붙이는 컴포넌트
///
/// 프리팹 구조:
/// Projectile
/// ├── NetworkIdentity   ← 필수
/// ├── Rigidbody         ← useGravity false, isKinematic true
/// ├── SphereCollider    ← isTrigger true
/// └── ProjectileBase    ← 이 스크립트
///
/// NetworkManager → Registered Spawnable Prefabs 에 등록 필수
/// </summary>
public class ProjectileBase : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] public float speed = 15f;
    [SerializeField] public float lifeTime = 3f;

    private float damage;
    private GameObject owner;
    private Vector3 direction;

    /// <summary>
    /// 서버에서 Spawn 직후 호출
    /// </summary>
    public void Init(Vector3 dir, GameObject ownerObj, float dmg)
    {
        direction = dir.normalized;
        owner = ownerObj;
        damage = dmg;

        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        // 발사자 제외
        if (owner != null && other.transform.root.gameObject == owner) return;

        var damageable = other.transform.root.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damage, owner);

        other.transform.root
            .GetComponent<PlayerAnimationController>()
            ?.RpcPlayHit();

        DestroySelf();
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}