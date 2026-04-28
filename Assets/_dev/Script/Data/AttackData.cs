using UnityEngine;

[System.Serializable]
public class AttackData
{
    public string    attackName       = "공격";
    public float     damageMultiplier = 1f;
    public float     cooldown         = 0.8f;
    public LayerMask targetLayer;
    public AttackType attackType = AttackType.Melee;

    [Header("근접")]
    public float meleeRange = 2.5f;
    public float meleeAngle = 120f;

    [Header("투사체")]
    [Tooltip("행동(폭발/관통)은 프리팹 컴포넌트가 결정")]
    public GameObject projectilePrefab;
    public int        projectileCount = 1;
    [Tooltip("전체 퍼짐 각도 (3발·30도 → 좌15°~우15°)")]
    public float      spreadAngle     = 0f;

    [Header("돌진")]
    public float dashSpeed        = 15f;
    public float dashDuration     = 0.3f;
    [Tooltip("0이면 이동만, 양수면 종점에서 해당 반경 피해")]
    public float dashDamageRadius = 0f;

    [Header("상태이상")]
    public float knockbackForce = 0f;
}
