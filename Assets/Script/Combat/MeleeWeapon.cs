using Mirror;
using UnityEngine;

/// <summary>
/// 근접 무기 — 검사 타입
/// OverlapSphere + 부채꼴 각도 판정
/// 언리얼 UMeleeAttackAbility와 동일 구조
/// </summary>
public class MeleeWeapon : WeaponBase
{
    [Header("Melee Config")]
    [Tooltip("공격 부채꼴 각도 (전방 기준 ±arcAngle/2)")]
    [SerializeField] private float arcAngle = 120f;

    private PlayerAnimationController animController;

    protected override void Awake()
    {
        base.Awake();
        animController = GetComponent<PlayerAnimationController>();
    }

    protected override void OnAttack(Vector3 origin, Vector3 direction)
    {
        // 로컬 즉시 애니 (레이턴시 보상)
        animController?.PlayAttackLocal();

        if (isLocalPlayer)
            CmdMeleeAttack(origin, direction);
    }

    [Command]
    private void CmdMeleeAttack(Vector3 origin, Vector3 dir)
    {
        float finalDamage = GetFinalDamage();
        Vector3 center    = origin + dir * (range * 0.5f);
        Collider[] hits   = Physics.OverlapSphere(center, range * 0.5f, targetLayer);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            // 부채꼴 각도 체크
            Vector3 toTarget = (hit.transform.position - origin).normalized;
            if (Vector3.Angle(dir, toTarget) > arcAngle * 0.5f) continue;

            if (hit.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(finalDamage, gameObject);

            hit.GetComponent<PlayerAnimationController>()?.RpcPlayHit();
        }

        animController?.RpcPlayAttack();
        RpcMeleeEffect(origin, dir);
    }

    [ClientRpc]
    private void RpcMeleeEffect(Vector3 origin, Vector3 dir)
    {
        // 이펙트 스폰 위치 — 전방 range 절반 지점
        Debug.Log($"[MELEE] 공격 이펙트 pos={origin + dir * range * 0.5f}");
    }
}
