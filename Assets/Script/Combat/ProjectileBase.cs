using Mirror;
using UnityEngine;

/// <summary>
/// 투사체 프리팹에 붙이는 컴포넌트
/// NetworkBehaviour → 서버에서 Spawn → 모든 클라이언트 동기화
/// 언리얼 AProjectile 대응
/// </summary>
public class ProjectileBase : NetworkBehaviour
{
    [Header("Projectile Config")]
    [SerializeField] public float speed    = 15f;
    [SerializeField] public float lifeTime = 3f;

    private float      damage;
    private GameObject owner;
    private Vector3    direction;

    /// <summary>
    /// 서버에서 Spawn 직후 호출 — 발사자와 방향, 데미지 주입
    /// </summary>
    public void Init(Vector3 dir, GameObject ownerObj, float dmg)
    {
        direction = dir.normalized;
        owner     = ownerObj;
        damage    = dmg;

        // 수명이 지나면 서버에서 제거 → 모든 클라이언트 동기화
        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation  = Quaternion.LookRotation(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer)  return;
        if (owner != null && other.gameObject == owner) return;

        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage, owner);

        other.GetComponent<PlayerAnimationController>()?.RpcPlayHit();

        DestroySelf();
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}
