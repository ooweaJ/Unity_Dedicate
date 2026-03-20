using Mirror;
using UnityEngine;

/// <summary>
/// 투사체 발사 무기 — 마법사 타입
/// 서버에서 ProjectileBase 스폰 → 모든 클라이언트 동기화
/// 언리얼 UProjectileAbility와 동일 구조
/// </summary>
public class ProjectileWeapon : WeaponBase
{
    [Header("Projectile Config")]
    [Tooltip("반드시 NetworkIdentity 컴포넌트 포함된 프리팹")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform  firePoint; // 발사 위치 (null이면 캐릭터 중심)

    private PlayerAnimationController animController;

    protected override void Awake()
    {
        base.Awake();
        animController = GetComponent<PlayerAnimationController>();
    }

    protected override void OnAttack(Vector3 origin, Vector3 direction)
    {
        animController?.PlayAttackLocal();

        if (isLocalPlayer)
            CmdFireProjectile(origin, direction);
    }

    [Command]
    private void CmdFireProjectile(Vector3 origin, Vector3 dir)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[PROJECTILE] projectilePrefab이 없습니다!");
            return;
        }

        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : origin + Vector3.up * 0.5f + dir * 0.5f;

        GameObject obj = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.LookRotation(dir)
        );

        // 데미지는 CharacterStats 기반으로 계산 후 투사체에 주입
        obj.GetComponent<ProjectileBase>()?.Init(dir, gameObject, GetFinalDamage());

        // 서버에서 스폰 → 모든 클라이언트에 자동 동기화
        NetworkServer.Spawn(obj);

        animController?.RpcPlayAttack();
    }
}
