using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// 전투 판정 핵심 컴포넌트
///
/// 슬롯: index 0 = 기본공격 / 1 = 스킬1 / 2 = 스킬2
/// 각 슬롯은 SkillData(cooldown + List&lt;SkillAction&gt;)로 정의되며,
/// action이 순서대로 실행됨 (Dash→Melee 조합 등 자유롭게 구성 가능)
/// </summary>
public class CharacterWeapon : NetworkBehaviour
{
    [SerializeField] private SkillData[] skills = new SkillData[3];

    private CharacterStats            stats;
    private PlayerAnimationController anim;
    private PlayerMovement            movement;

    private float lastBasicTime  = -99f;
    private float lastSkill1Time = -99f;
    private float lastSkill2Time = -99f;

    private float svrBasicTime  = -99f;
    private float svrSkill1Time = -99f;
    private float svrSkill2Time = -99f;

    // 입력 버퍼 — 쿨다운 직전 입력을 0.15초 보존 후 재시도 (조준 방향도 함께 저장)
    private float   _bufAttackTime = -99f;
    private float   _bufSkill1Time = -99f;
    private float   _bufSkill2Time = -99f;
    private Vector3 _bufAttackDir  = Vector3.forward;
    private Vector3 _bufSkill1Dir  = Vector3.forward;
    private Vector3 _bufSkill2Dir  = Vector3.forward;
    private Vector3 _bufAttackTarget;
    private Vector3 _bufSkill1Target;
    private Vector3 _bufSkill2Target;
    private const float InputBufferWindow = 0.15f;

    private StatusEffectHandler effectHandler;

    private void Awake()
    {
        stats         = GetComponent<CharacterStats>();
        anim          = GetComponent<PlayerAnimationController>();
        movement      = GetComponent<PlayerMovement>();
        effectHandler = GetComponent<StatusEffectHandler>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        TryFlushBuffer(ref _bufAttackTime, ref _bufAttackDir, ref _bufAttackTarget, 0, ref lastBasicTime);
        TryFlushBuffer(ref _bufSkill1Time, ref _bufSkill1Dir, ref _bufSkill1Target, 1, ref lastSkill1Time);
        TryFlushBuffer(ref _bufSkill2Time, ref _bufSkill2Dir, ref _bufSkill2Target, 2, ref lastSkill2Time);
    }

    private void TryFlushBuffer(ref float bufTime, ref Vector3 bufDir, ref Vector3 bufTarget,
                                 int skillIndex, ref float lastTime)
    {
        if (Time.time - bufTime > InputBufferWindow) return;
        if (!CanUse(skillIndex, ref lastTime)) return;
        bufTime = -99f;
        CmdUseAttack(transform.position, bufDir, bufTarget, skillIndex);
    }

    public void Setup(SkillData[] skillData)
    {
        skills = skillData;
    }

    // ─── 공개 API (PlayerCombat에서 호출) ────────────────────────────────
    public void UseBasicAttack(Vector3 aimDir, float magnitude = 1f)
    {
        if (!CanUse(0, ref lastBasicTime))
        {
            if (isLocalPlayer && GetSkill(0) != null)
            {
                _bufAttackTime   = Time.time;
                _bufAttackDir    = aimDir;
                _bufAttackTarget = ComputeTargetPos(0, aimDir, magnitude);
            }
            return;
        }
        _bufAttackTime = -99f;
        CmdUseAttack(transform.position, aimDir, ComputeTargetPos(0, aimDir, magnitude), 0);
    }

    public void UseSkill1Attack(Vector3 aimDir, float magnitude = 1f)
    {
        if (!CanUse(1, ref lastSkill1Time))
        {
            if (isLocalPlayer && GetSkill(1) != null)
            {
                _bufSkill1Time   = Time.time;
                _bufSkill1Dir    = aimDir;
                _bufSkill1Target = ComputeTargetPos(1, aimDir, magnitude);
            }
            return;
        }
        _bufSkill1Time = -99f;
        CmdUseAttack(transform.position, aimDir, ComputeTargetPos(1, aimDir, magnitude), 1);
    }

    public void UseSkill2Attack(Vector3 aimDir, float magnitude = 1f)
    {
        if (!CanUse(2, ref lastSkill2Time))
        {
            if (isLocalPlayer && GetSkill(2) != null)
            {
                _bufSkill2Time   = Time.time;
                _bufSkill2Dir    = aimDir;
                _bufSkill2Target = ComputeTargetPos(2, aimDir, magnitude);
            }
            return;
        }
        _bufSkill2Time = -99f;
        CmdUseAttack(transform.position, aimDir, ComputeTargetPos(2, aimDir, magnitude), 2);
    }

    // TargetPoint 스킬: 조이스틱 magnitude로 착탄거리 결정
    private Vector3 ComputeTargetPos(int skillIndex, Vector3 aimDir, float magnitude)
    {
        var skill = GetSkill(skillIndex);
        if (skill == null || skill.inputType != SkillInputType.TargetPoint)
            return Vector3.zero;

        float maxDist = 10f;
        if (skill.actions != null)
        {
            foreach (var action in skill.actions)
            {
                if (action.actionType == SkillActionType.Projectile &&
                    action.projectileData is ExplosiveProjectileDataSO expData)
                {
                    maxDist = expData.maxDistance;
                    break;
                }
            }
        }
        return transform.position + aimDir * (maxDist * Mathf.Clamp01(magnitude));
    }

    private bool CanUse(int skillIndex, ref float lastTime)
    {
        if (!isLocalPlayer)                          { Debug.Log("[WEAPON] CanUse 실패: 로컬 플레이어 아님");             return false; }
        if (effectHandler != null && effectHandler.IsStunned) { Debug.Log("[WEAPON] CanUse 실패: 스턴 상태");                  return false; }
        var skill = GetSkill(skillIndex);
        if (skill == null)                           { Debug.Log("[WEAPON] CanUse 실패: SkillData null (Setup 안됨?)"); return false; }
        float remain = skill.cooldown - (Time.time - lastTime);
        if (remain > 0f)                             { Debug.Log($"[WEAPON] CanUse 실패: 쿨다운 {remain:F2}초 남음");   return false; }
        lastTime = Time.time;
        return true;
    }

    // 쿨다운 비율 반환 (0=준비완료, 1=가득참) — MobileCombatUI 쿨타임 UI용
    public float GetCooldownRatio(int skillIndex)
    {
        var skill = GetSkill(skillIndex);
        if (skill == null || skill.cooldown <= 0f) return 0f;
        float lastTime = skillIndex == 0 ? lastBasicTime : skillIndex == 1 ? lastSkill1Time : lastSkill2Time;
        return 1f - Mathf.Clamp01((Time.time - lastTime) / skill.cooldown);
    }

    // HitBox에서 참조
    public float GetFinalDamage(int skillIndex = 0)
    {
        var skill = GetSkill(skillIndex);
        if (skill == null || skill.actions == null || skill.actions.Count == 0) return 20f;
        float baseAtk = stats != null ? stats.FinalAttack : 20f;
        return baseAtk * skill.actions[0].damageMultiplier;
    }

    // ─── Command ──────────────────────────────────────────────────────────
    [Command]
    private void CmdUseAttack(Vector3 origin, Vector3 aimDir, Vector3 targetPos, int skillIndex)
    {
        if (!ServerCheckCooldown(skillIndex, GetSkill(skillIndex)))
        {
            Debug.Log($"[WEAPON] 서버 쿨다운 차단: index={skillIndex}");
            return;
        }

        Vector3 flatDir = new Vector3(aimDir.x, 0f, aimDir.z).normalized;
        if (flatDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(flatDir);
            RpcFaceDirection(flatDir);
        }

        BroadcastAnimation(skillIndex);
        StartCoroutine(ExecuteSkill(origin, aimDir, targetPos, skillIndex));
    }

    [ClientRpc]
    private void RpcFaceDirection(Vector3 dir)
    {
        transform.rotation = Quaternion.LookRotation(dir);
    }

    [Server]
    private IEnumerator ExecuteSkill(Vector3 origin, Vector3 aimDir, Vector3 targetPos, int skillIndex)
    {
        var skill = GetSkill(skillIndex);
        if (skill?.actions == null) yield break;

        Vector3 nDir = aimDir.normalized;

        foreach (var action in skill.actions)
        {
            if (action.delay > 0f) yield return new WaitForSeconds(action.delay);

            float dmg = (stats != null ? stats.FinalAttack : 20f) * action.damageMultiplier;
            Debug.Log($"[WEAPON] action 실행 | skill={skillIndex} type={action.actionType} dmg={dmg:F1}");

            switch (action.actionType)
            {
                case SkillActionType.Melee:      PerformMelee(origin, nDir, action, dmg);            break;
                case SkillActionType.Projectile: PerformProjectile(origin, nDir, targetPos, action, dmg); break;
                case SkillActionType.Dash:       PerformDash(nDir, action, dmg);                     break;
            }
        }
    }

    private bool ServerCheckCooldown(int index, SkillData skill)
    {
        if (skill == null) return false;
        float now  = (float)NetworkTime.time;
        float last = index == 0 ? svrBasicTime : index == 1 ? svrSkill1Time : svrSkill2Time;
        if (now - last < skill.cooldown) return false;
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
    private void PerformMelee(Vector3 origin, Vector3 dir, SkillAction action, float dmg)
    {
        Vector3    center   = origin + dir * (action.meleeRange * 0.5f);
        Collider[] hits     = Physics.OverlapSphere(center, action.meleeRange * 0.5f, action.targetLayer);
        bool       hitEnemy = false;

        foreach (var hit in hits)
        {
            if (hit.transform.root.gameObject == gameObject) continue;
            if (Vector3.Angle(dir, (hit.transform.position - origin).normalized) > action.meleeAngle * 0.5f) continue;

            var info = new DamageInfo(dmg, gameObject, dir, action.onHitEffect);
            hit.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
            hit.transform.root.GetComponent<PlayerAnimationController>()
                ?.RpcPlayHit(hit.transform.position, action.hitEffect);
            hitEnemy = true;
        }

        if (hitEnemy) GetComponent<PlayerBushState>()?.RevealTemporarily();
    }

    // ─── 투사체 ───────────────────────────────────────────────────────────
    [Server]
    private void PerformProjectile(Vector3 origin, Vector3 dir, Vector3 targetPos,
                                    SkillAction action, float dmg)
    {
        var data = action.projectileData;
        if (data == null || data.visualPrefab == null)
        {
            Debug.LogError($"[WEAPON] {action.actionName}: ProjectileDataSO 또는 prefab 미연결");
            return;
        }

        var behaviorPrefab = ProjectileManager.Instance?.GetBehaviorPrefab(data.projectileType);
        if (behaviorPrefab == null)
        {
            Debug.LogError($"[WEAPON] ProjectileManager에 {data.projectileType} 프리팹 미등록");
            return;
        }

        bool isTargetReached = data is ExplosiveProjectileDataSO expSOCheck &&
                               expSOCheck.trigger == ExplosionTrigger.OnTargetReached;

        int count = Mathf.Max(1, data.count);

        for (int i = 0; i < count; i++)
        {
            float   t        = count == 1 ? 0f : (float)i / (count - 1) - 0.5f;
            Vector3 spawnPos = origin + Vector3.up * 0.5f;

            Vector3 shotDir;
            Vector3 clampedTarget = targetPos;

            if (isTargetReached && targetPos != Vector3.zero && data is ExplosiveProjectileDataSO expSO)
            {
                // XZ 거리로 최대 사거리 클램프
                Vector3 toTarget = new Vector3(targetPos.x - spawnPos.x, 0f, targetPos.z - spawnPos.z);
                if (toTarget.magnitude > expSO.maxDistance)
                    clampedTarget = spawnPos + toTarget.normalized * expSO.maxDistance;
                clampedTarget.y = spawnPos.y;

                shotDir = new Vector3(clampedTarget.x - spawnPos.x, 0f, clampedTarget.z - spawnPos.z).normalized;
            }
            else
            {
                shotDir = Quaternion.AngleAxis(t * data.spreadAngle, Vector3.up) * dir;
            }

            GameObject obj  = Instantiate(behaviorPrefab, spawnPos, Quaternion.LookRotation(shotDir));
            var        proj = obj.GetComponent<ProjectileBase>();
            if (proj == null) { Destroy(obj); continue; }

            proj.Init(shotDir, gameObject, dmg, data);

            // SetTargetPosition은 NetworkServer.Spawn 이전에 호출 — 초기 SyncVar 값으로 클라이언트에 전달
            if (isTargetReached && proj is ExplosiveProjectile expProj)
                expProj.SetTargetPosition(clampedTarget);

            NetworkServer.Spawn(obj);
        }
    }

    // ─── 돌진 ─────────────────────────────────────────────────────────────
    [Server]
    private void PerformDash(Vector3 dir, SkillAction action, float dmg)
    {
        RpcStartDash(dir, action.dashSpeed, action.dashDuration);
        if (action.dashDamageRadius > 0f)
            StartCoroutine(DashDamageAfterDelay(dir, action, dmg));
    }

    private IEnumerator DashDamageAfterDelay(Vector3 dir, SkillAction action, float dmg)
    {
        yield return new WaitForSeconds(action.dashDuration);

        Collider[] hits     = Physics.OverlapSphere(transform.position, action.dashDamageRadius, action.targetLayer);
        bool       hitEnemy = false;

        foreach (var hit in hits)
        {
            if (hit.transform.root.gameObject == gameObject) continue;
            var info = new DamageInfo(dmg, gameObject, dir, action.onHitEffect);
            hit.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
            hit.transform.root.GetComponent<PlayerAnimationController>()
                ?.RpcPlayHit(hit.transform.position, action.hitEffect);
            hitEnemy = true;
        }

        if (hitEnemy) GetComponent<PlayerBushState>()?.RevealTemporarily();
    }

    [ClientRpc]
    private void RpcStartDash(Vector3 dir, float speed, float duration)
    {
        if (isLocalPlayer) movement?.StartDash(dir, speed, duration);
    }

    [Server]
    private void BroadcastAnimation(int skillIndex)
    {
        switch (skillIndex)
        {
            case 0: anim?.RpcPlayAttack(); break;
            case 1: anim?.RpcPlaySkill();  break;
            case 2: anim?.RpcPlaySkill2(); break;
        }
    }

    private SkillData GetSkill(int index)
    {
        if (skills == null || index < 0 || index >= skills.Length) return null;
        return skills[index];
    }
}
