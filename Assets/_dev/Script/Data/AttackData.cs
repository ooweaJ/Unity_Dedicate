using UnityEngine;

[System.Serializable]
public class SkillAction
{
    public string          actionName       = "행동";
    [Tooltip("이전 action 완료 후 대기 시간 (0이면 즉시)")]
    public float           delay            = 0f;
    public float           damageMultiplier = 1f;
    public LayerMask       targetLayer;
    public SkillActionType actionType = SkillActionType.Melee;

    [Header("근접")]
    public float meleeRange = 2.5f;
    public float meleeAngle = 120f;

    [Header("투사체")]
    public ProjectileDataSO projectileData;

    [Header("돌진")]
    public float dashSpeed        = 15f;
    public float dashDuration     = 0.3f;
    [Tooltip("0이면 이동만, 양수면 종점에서 해당 반경 피해")]
    public float dashDamageRadius = 0f;

    [Header("상태이상 (근접/돌진)")]
    [Tooltip("투사체는 ProjectileDataSO.onHitEffect 사용")]
    public StatusEffect onHitEffect;

    [Header("이펙트 (근접/돌진)")]
    [Tooltip("투사체는 ProjectileDataSO.hitEffect 사용")]
    public EffectType hitEffect = EffectType.MeleeHit;
}
