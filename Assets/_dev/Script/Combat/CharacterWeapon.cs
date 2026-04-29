using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// 전투 판정 핵심 컴포넌트
///
/// 슬롯: index 0 = basicAttack / 1 = skill1Attack / 2 = skill2Attack
/// 타입: Melee(OverlapSphere+각도) / Projectile(멀티샷 Spawn) / Dash(돌진+종점피해)
/// 넉백은 DamageInfo.Knockback을 통해 PlayerStats.TakeDamage에서 처리
/// </summary>
public class CharacterWeapon : NetworkBehaviour
{
    [SerializeField] private AttackData basicAttack;
    [SerializeField] private AttackData skill1Attack;
    [SerializeField] private AttackData skill2Attack;

    private CharacterStats            stats;
    private PlayerAnimationController anim;
    private PlayerMovement            movement;

    private float lastBasicTime  = -99f;
    private float lastSkill1Time = -99f;
    private float lastSkill2Time = -99f;

    private float svrBasicTime  = -99f;
    private float svrSkill1Time = -99f;
    private float svrSkill2Time = -99f;

    private void Awake()
    {
        stats    = GetComponent<CharacterStats>();
        anim     = GetComponent<PlayerAnimationController>();
        movement = GetComponent<PlayerMovement>();
    }

    public void Setup(AttackData basic, AttackData skill1, AttackData skill2)
    {
        basicAttack  = basic;
        skill1Attack = skill1;
        skill2Attack = skill2;
    }

    // ─── 공개 API (PlayerCombat에서 호출) ────────────────────────────────
    public void UseBasicAttack()
    {
        if (!CanUse(basicAttack, ref lastBasicTime)) return;
        CmdUseAttack(transform.position, transform.forward, 0);
    }

    public void UseSkill1Attack()
    {
        if (!CanUse(skill1Attack, ref lastSkill1Time)) return;
        CmdUseAttack(transform.position, transform.forward, 1);
    }

    public void UseSkill2Attack()
    {
        if (!CanUse(skill2Attack, ref lastSkill2Time)) return;
        CmdUseAttack(transform.position, transform.forward, 2);
    }

    private bool CanUse(AttackData data, ref float lastTime)
    {
        if (!isLocalPlayer) { Debug.Log("[WEAPON] CanUse 실패: 로컬 플레이어 아님");           return false; }
        if (data == null)   { Debug.Log("[WEAPON] CanUse 실패: AttackData null (Setup 안됨?)"); return false; }
        float remain = data.cooldown - (Time.time - lastTime);
        if (remain > 0f)    { Debug.Log($"[WEAPON] CanUse 실패: 쿨다운 {remain:F2}초 남음");   return false; }
        lastTime = Time.time;
        return true;
    }

    // HitBox에서 참조
    public float GetFinalDamage(int attackIndex = 0)
    {
        var data = GetData(attackIndex);
        if (data == null) return 20f;
        float baseAtk = stats != null ? stats.FinalAttack : 20f;
        return baseAtk * data.damageMultiplier;
    }

    // ─── Command ──────────────────────────────────────────────────────────
    [Command]
    private void CmdUseAttack(Vector3 origin, Vector3 dir, int attackIndex)
    {
        if (!ServerCheckCooldown(attackIndex, GetData(attackIndex)))
        {
            Debug.Log($"[WEAPON] 서버 쿨다운 차단: index={attackIndex}");
            return;
        }
        RunAttack(origin, dir, attackIndex);
    }

    private void RunAttack(Vector3 origin, Vector3 dir, int attackIndex)
    {
        var data = GetData(attackIndex);
        if (data == null) { Debug.LogError($"[WEAPON] index {attackIndex} AttackData null"); return; }

        float   dmg  = GetFinalDamage(attackIndex);
        Vector3 nDir = dir.normalized;

        Debug.Log($"[WEAPON] 공격 실행 | index={attackIndex} type={data.attackType} dmg={dmg:F1}");

        switch (data.attackType)
        {
            case AttackType.Melee:      PerformMelee(origin, nDir, data, dmg);      break;
            case AttackType.Projectile: PerformProjectile(origin, nDir, data, dmg); break;
            case AttackType.Dash:       PerformDash(nDir, data, dmg);               break;
        }

        BroadcastAnimation(attackIndex);
    }

    private bool ServerCheckCooldown(int index, AttackData data)
    {
        float now  = (float)NetworkTime.time;
        float last = index == 0 ? svrBasicTime : index == 1 ? svrSkill1Time : svrSkill2Time;
        if (now - last < data.cooldown) return false;

        switch (index)
        {
            case 0: svrBasicTime  = now; break;
            case 1: svrSkill1Time = now; break;
            case 2: svrSkill2Time = now; break;
        }
        return true;
    }

    // ─── 근접 ─────────────────────────────────────────────────────────────
    [Server]
    private void PerformMelee(Vector3 origin, Vector3 dir, AttackData data, float dmg)
    {
        Vector3    center = origin + dir * (data.meleeRange * 0.5f);
        Collider[] hits   = Physics.OverlapSphere(center, data.meleeRange * 0.5f, data.targetLayer);

        foreach (var hit in hits)
        {
            if (hit.transform.root.gameObject == gameObject) continue;
            if (Vector3.Angle(dir, (hit.transform.position - origin).normalized) > data.meleeAngle * 0.5f) continue;

            var info = new DamageInfo(dmg, gameObject, dir, data.knockbackForce);
            hit.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
            hit.transform.root.GetComponent<PlayerAnimationController>()
                ?.RpcPlayHit(hit.transform.position, data.hitEffect);
        }
    }

    // ─── 투사체 (멀티샷) ───────────────────────────────────────────────────
    [Server]
    private void PerformProjectile(Vector3 origin, Vector3 dir, AttackData data, float dmg)
    {
        if (data.projectilePrefab == null)
        {
            Debug.LogError($"[WEAPON] {data.attackName}: projectilePrefab이 null — AttackData 인스펙터 확인");
            return;
        }

        int count = Mathf.Max(1, data.projectileCount);
        Debug.Log($"[WEAPON] 투사체 스폰: {count}발 / 퍼짐={data.spreadAngle}°");

        for (int i = 0; i < count; i++)
        {
            float   t        = count == 1 ? 0f : (float)i / (count - 1) - 0.5f;
            Vector3 shotDir  = Quaternion.AngleAxis(t * data.spreadAngle, Vector3.up) * dir;
            Vector3 spawnPos = origin + Vector3.up * 0.5f + shotDir * 0.5f;

            GameObject obj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.LookRotation(shotDir));
            var proj = obj.GetComponent<ProjectileBase>();
            if (proj == null)
            {
                Debug.LogError($"[WEAPON] projectilePrefab '{data.projectilePrefab.name}'에 ProjectileBase 컴포넌트 없음");
                Destroy(obj);
                continue;
            }
            proj.Init(shotDir, gameObject, dmg, data.knockbackForce);
            NetworkServer.Spawn(obj);
            Debug.Log($"[WEAPON] 투사체 {i + 1}번 스폰 완료: pos={spawnPos}");
        }
    }

    // ─── 돌진 ─────────────────────────────────────────────────────────────
    [Server]
    private void PerformDash(Vector3 dir, AttackData data, float dmg)
    {
        RpcStartDash(dir, data.dashSpeed, data.dashDuration);
        if (data.dashDamageRadius > 0f)
            StartCoroutine(DashDamageAfterDelay(dir, data, dmg));
    }

    private IEnumerator DashDamageAfterDelay(Vector3 dir, AttackData data, float dmg)
    {
        yield return new WaitForSeconds(data.dashDuration);

        Collider[] hits = Physics.OverlapSphere(transform.position, data.dashDamageRadius, data.targetLayer);
        foreach (var hit in hits)
        {
            if (hit.transform.root.gameObject == gameObject) continue;
            var info = new DamageInfo(dmg, gameObject, dir, data.knockbackForce);
            hit.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
            hit.transform.root.GetComponent<PlayerAnimationController>()
                ?.RpcPlayHit(hit.transform.position, data.hitEffect);
        }
    }

    [ClientRpc]
    private void RpcStartDash(Vector3 dir, float speed, float duration)
    {
        if (isLocalPlayer) movement?.StartDash(dir, speed, duration);
    }

    [Server]
    private void BroadcastAnimation(int attackIndex)
    {
        switch (attackIndex)
        {
            case 0: anim?.RpcPlayAttack(); break;
            case 1: anim?.RpcPlaySkill();  break;
            case 2: anim?.RpcPlaySkill2(); break;
        }
    }

    private AttackData GetData(int index)
    {
        switch (index)
        {
            case 0:  return basicAttack;
            case 1:  return skill1Attack;
            case 2:  return skill2Attack;
            default: return null;
        }
    }
}
