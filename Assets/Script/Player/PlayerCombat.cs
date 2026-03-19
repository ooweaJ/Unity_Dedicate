using Mirror;
using UnityEngine;

/// <summary>
/// 언리얼 AnimMontage 대응 구조:
/// - Animator Layer 1 (Action Layer) + Avatar Mask으로 상체만 오버라이드
/// - AnimationEvent로 히트박스 활성화 타이밍 제어
/// - Command(클라이언트→서버 판정) + ClientRpc(서버→전체 이펙트)
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 0.8f;

    [Header("Skill")]
    [SerializeField] private float skillDamage = 60f;
    [SerializeField] private float skillRange = 5f;
    [SerializeField] private float skillCooldown = 6f;

    [Header("Effects")]
    [SerializeField] private GameObject attackFxPrefab;
    [SerializeField] private GameObject skillFxPrefab;
    [SerializeField] private LayerMask enemyLayer;

    private PlayerAnimationController animController;

    // 클라이언트 쿨다운 표시용 (실제 검증은 서버)
    private float lastAttackTime = -99f;
    private float lastSkillTime = -99f;

    // 서버 전용 쿨다운 검증
    [SyncVar] private double serverLastAttackTime;
    [SyncVar] private double serverLastSkillTime;

    private void Awake()
    {
        animController = GetComponent<PlayerAnimationController>();
    }

    // ─── 입력 진입점 (PlayerController에서 호출) ─────────────────────────
    public void OnAttackInput()
    {
        if (!isLocalPlayer) return;
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        // 레이턴시 보상: 로컬에서 즉시 애니 재생
        animController.PlayAttackLocal();

        // 서버에 판정 요청
        CmdRequestAttack(transform.position, transform.forward);
    }

    public void OnSkillInput()
    {
        if (!isLocalPlayer) return;
        if (Time.time - lastSkillTime < skillCooldown) return;

        lastSkillTime = Time.time;

        animController.PlaySkillLocal();
        CmdRequestSkill(transform.position, transform.forward);
    }

    // ─── Command: 클라이언트 → 서버 판정 ─────────────────────────────────
    [Command]
    private void CmdRequestAttack(Vector3 origin, Vector3 direction)
    {
        // 서버 쿨다운 검증 (치팅 방지)
        if (NetworkTime.time - serverLastAttackTime < attackCooldown) return;
        serverLastAttackTime = NetworkTime.time;

        PerformAttack(origin, direction, attackRange, attackDamage);

        // 공격 애니+이펙트를 모든 클라이언트에 브로드캐스트
        // (몽타주 PlayMontage → Multicast 대응)
        animController.RpcPlayAttack();
        RpcSpawnAttackFx(origin + direction * attackRange * 0.5f,
                         Quaternion.LookRotation(direction));
    }

    [Command]
    private void CmdRequestSkill(Vector3 origin, Vector3 direction)
    {
        if (NetworkTime.time - serverLastSkillTime < skillCooldown) return;
        serverLastSkillTime = NetworkTime.time;

        PerformAttack(origin, direction, skillRange, skillDamage);

        animController.RpcPlaySkill();
        RpcSpawnSkillFx(origin + direction * skillRange * 0.5f,
                        Quaternion.LookRotation(direction));
    }

    // ─── 서버 전용 피격 판정 ─────────────────────────────────────────────
    [Server]
    private void PerformAttack(Vector3 origin, Vector3 dir, float range, float damage)
    {
        // 부채꼴 범위 판정 (브롤스타즈식 전방 OverlapSphere)
        Vector3 center = origin + dir * (range * 0.5f);
        Collider[] hits = Physics.OverlapSphere(center, range * 0.5f, enemyLayer);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<PlayerStats>(out var target)) continue;
            if (hit.gameObject == gameObject) continue; // 자기 자신 제외

            target.TakeDamage(damage);
            // 피격 대상 애니메이션 (대상의 PlayerAnimationController 호출)
            hit.GetComponent<PlayerAnimationController>()?.RpcPlayHit();
        }
    }

    // ─── ClientRpc: 이펙트 스폰 ──────────────────────────────────────────
    [ClientRpc]
    private void RpcSpawnAttackFx(Vector3 pos, Quaternion rot)
    {
        if (attackFxPrefab == null) return;
        Destroy(Instantiate(attackFxPrefab, pos, rot), 1.5f);
    }

    [ClientRpc]
    private void RpcSpawnSkillFx(Vector3 pos, Quaternion rot)
    {
        if (skillFxPrefab == null) return;
        Destroy(Instantiate(skillFxPrefab, pos, rot), 2.5f);
    }
}