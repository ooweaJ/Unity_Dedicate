using UnityEngine;

/// <summary>
/// 무기 데이터 ScriptableObject
/// Project에서 우클릭 → Create → Game/Weapon Data로 생성
/// 무기마다 하나씩 에셋 파일 만들어서 관리
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponName = "기본 무기";
    public Sprite weaponIcon;                // UI 아이콘
    public GameObject weaponMeshPrefab;       // 무기 외형 프리팹 (WeaponRoot에 붙임)

    [Header("공격 스탯")]
    public float damageMultiplier = 1.0f;     // CharacterStats.FinalAttack × 이 값
    public float attackCooldown = 0.8f;
    public float attackRange = 2.5f;
    public LayerMask targetLayer;

    [Header("공격 타입")]
    public AttackType attackType = AttackType.Melee;

    [Header("근접 전용")]
    public float arcAngle = 120f;

    [Header("원거리 전용")]
    public GameObject projectilePrefab;       // NetworkIdentity 포함 프리팹
    public float projectileSpeed = 15f;
    public Transform firePoint;              // null이면 캐릭터 중심에서 발사
}

public enum AttackType { Melee, Projectile }