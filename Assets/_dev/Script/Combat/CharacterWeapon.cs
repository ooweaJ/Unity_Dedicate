using Mirror;
using UnityEngine;

/// <summary>
/// 기존 WeaponBase/MeleeWeapon/ProjectileWeapon 통합 대체 컴포넌트
/// Player 프리팹에 붙임
///
/// SO 구조:
/// AttackDataSO.attackType == Melee      → OverlapSphere 근접 판정
/// AttackDataSO.attackType == Projectile → projectilePrefab 서버 Spawn
///
/// 검사 기본공격(근접) + 스킬(칼 던지기=투사체)도
/// AttackDataSO 두 개만 다르게 연결하면 끝
/// </summary>
public class CharacterWeapon : NetworkBehaviour
{
    // CharacterInitializer에서 주입 — 인스펙터 직접 연결도 가능
    [SerializeField] private AttackDataSO basicAttackData;
    [SerializeField] private AttackDataSO skillAttackData;

    private CharacterStats              stats;
    private PlayerAnimationController   anim;

    private float lastBasicTime = -99f;
    private float lastSkillTime = -99f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        anim  = GetComponent<PlayerAnimationController>();
    }

    // CharacterInitializer에서 런타임 주입
    public void Setup(AttackDataSO basic, AttackDataSO skill)
    {
        basicAttackData = basic;
        skillAttackData = skill;
    }

    // ─── PlayerCombat에서 호출 ────────────────────────────────────────
    public void UseBasicAttack()
    {
        if (basicAttackData == null) return;
        if (Time.time - lastBasicTime < basicAttackData.cooldown) return;
        lastBasicTime = Time.time;

        if (isLocalPlayer)
            CmdUseAttack(transform.position, transform.forward, isSkill: false);
    }

    public void UseSkillAttack()
    {
        if (skillAttackData == null) return;
        if (Time.time - lastSkillTime < skillAttackData.cooldown) return;
        lastSkillTime = Time.time;

        anim?.PlaySkillLocal();

        if (isLocalPlayer)
            CmdUseAttack(transform.position, transform.forward, isSkill: true);
    }

    // ─── 데미지 계산 (HitBox에서도 참조) ─────────────────────────────
    public float GetFinalDamage(bool isSkill = false)
    {
        var data = isSkill ? skillAttackData : basicAttackData;
        if (data == null) return 20f;
        float baseAtk = stats != null ? stats.FinalAttack : 20f;
        return baseAtk * data.damageMultiplier;
    }

    // ─── Command: 클라이언트 → 서버 판정 ─────────────────────────────
    [Command]
    private void CmdUseAttack(Vector3 origin, Vector3 dir, bool isSkill)
    {
        var data = isSkill ? skillAttackData : basicAttackData;
        if (data == null) return;

        float dmg = GetFinalDamage(isSkill);

        if (data.attackType == AttackType.Melee)
            PerformMelee(origin, dir, data, dmg);
        else
            PerformProjectile(origin, dir, data, dmg);

        // 애니메이션 브로드캐스트
        if (isSkill) anim?.RpcPlaySkill();
        else         anim?.RpcPlayAttack();
    }

    // ─── 서버: 근접 판정 ─────────────────────────────────────────────
    [Server]
    private void PerformMelee(Vector3 origin, Vector3 dir, AttackDataSO data, float dmg)
    {
        Vector3    center = origin + dir * (data.meleeRange * 0.5f);
        Collider[] hits   = Physics.OverlapSphere(
            center, data.meleeRange * 0.5f, data.targetLayer);

        foreach (var hit in hits)
        {
            if (hit.transform.root.gameObject == gameObject) continue;

            // 부채꼴 각도 체크
            Vector3 toTarget = (hit.transform.position - origin).normalized;
            if (Vector3.Angle(dir, toTarget) > data.meleeAngle * 0.5f) continue;

            hit.transform.root.GetComponent<IDamageable>()
                ?.TakeDamage(dmg, gameObject);
            hit.transform.root.GetComponent<PlayerAnimationController>()
                ?.RpcPlayHit();
        }
    }

    // ─── 서버: 투사체 스폰 ───────────────────────────────────────────
    [Server]
    private void PerformProjectile(Vector3 origin, Vector3 dir, AttackDataSO data, float dmg)
    {
        if (data.projectilePrefab == null)
        {
            Debug.LogError($"[WEAPON] {data.attackName}: projectilePrefab이 없습니다!");
            return;
        }

        Vector3    spawnPos = origin + Vector3.up * 0.5f + dir * 0.5f;
        GameObject obj      = Instantiate(
            data.projectilePrefab, spawnPos, Quaternion.LookRotation(dir));

        // ProjectileBase를 상속한 어떤 컴포넌트든 Init 호출
        // → StandardProjectile, ExplosiveProjectile 모두 동작
        obj.GetComponent<ProjectileBase>()?.Init(dir, gameObject, dmg);
        NetworkServer.Spawn(obj);
    }
}
