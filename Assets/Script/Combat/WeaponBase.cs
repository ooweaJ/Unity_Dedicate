using Mirror;
using UnityEngine;

/// <summary>
/// 모든 무기의 추상 기반 클래스
/// Template Method 패턴 — 공통 로직(쿨다운, 데미지 계산)은 여기서
/// 실제 판정 방식(근접/투사체)은 자식 클래스에서 구현
/// </summary>
public abstract class WeaponBase : NetworkBehaviour
{
    [Header("Weapon Config")]
    [Tooltip("무기 고유 데미지 배율. 평타=1.0, 강스킬=2.5 등")]
    [SerializeField] protected float damageMultiplier = 1.0f;
    [SerializeField] protected float cooldown         = 0.8f;
    [SerializeField] protected float range            = 2.5f;
    [SerializeField] protected LayerMask targetLayer;

    // CharacterStats는 같은 GameObject에 있는 컴포넌트에서 참조
    // 강화 레벨에 따른 최종 데미지가 자동 반영됨
    protected CharacterStats stats;

    // 서버/클라이언트 공용 쿨다운 (클라이언트 쿨다운 표시용)
    private float lastAttackTime = -99f;

    protected virtual void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    // ─── 외부 진입점 (PlayerCombat에서 호출) ─────────────────────────────
    public void Attack(Vector3 origin, Vector3 direction)
    {
        if (!CanAttack()) return;
        lastAttackTime = Time.time;
        OnAttack(origin, direction);
    }

    public bool CanAttack() => Time.time - lastAttackTime >= cooldown;

    // ─── 최종 데미지 계산 ─────────────────────────────────────────────────
    // CharacterStats 없으면 damageMultiplier를 기본값(20)으로 계산
    protected float GetFinalDamage()
    {
        if (stats != null)
            return stats.FinalAttack * damageMultiplier;

        Debug.LogWarning($"[WEAPON] {gameObject.name}에 CharacterStats 없음 — 기본값 사용");
        return 20f * damageMultiplier;
    }

    // ─── 자식 클래스에서 구현 (Template Method) ───────────────────────────
    protected abstract void OnAttack(Vector3 origin, Vector3 direction);
}
