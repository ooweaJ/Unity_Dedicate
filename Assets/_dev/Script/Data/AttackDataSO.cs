using UnityEngine;

/// <summary>
/// 공격 하나의 데이터 — 근접/투사체 공통
/// 검사 기본공격(근접), 검 던지기(투사체), 대포(투사체+폭발) 전부 이 하나로 관리
///
/// Project 우클릭 → Create → Game/Attack Data
/// </summary>
[CreateAssetMenu(fileName = "NewAttack", menuName = "Game/Attack Data")]
public class AttackDataSO : ScriptableObject
{
    [Header("공통")]
    public string    attackName       = "공격";
    public float     damageMultiplier = 1.0f;
    public float     cooldown         = 0.8f;
    public LayerMask targetLayer;

    [Header("공격 타입")]
    public AttackType attackType = AttackType.Melee;

    [Header("근접 전용")]
    public float meleeRange = 2.5f;
    public float meleeAngle = 120f;

    [Header("투사체 전용")]
    [Tooltip("반드시 NetworkIdentity 포함 프리팹. 실제 행동(폭발/관통)은 프리팹 컴포넌트가 결정")]
    public GameObject projectilePrefab;
}
