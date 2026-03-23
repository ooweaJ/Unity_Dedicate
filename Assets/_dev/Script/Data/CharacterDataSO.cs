using UnityEngine;

/// <summary>
/// 캐릭터 고유 데이터 — 스탯 + 무기 구성 + 외형
///
/// Project 우클릭 → Create → Game/Character Data
/// 캐릭터마다 에셋 파일 하나씩 생성 (Swordsman.asset, Mage.asset 등)
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string     characterName    = "캐릭터";

    [Header("외형")]
    [Tooltip("WeaponRoot에 붙일 무기 메시 프리팹")]
    public GameObject weaponMeshPrefab;

    [Header("기본 스탯")]
    public float baseAttack  = 20f;
    public float baseMaxHp   = 100f;
    public float baseDefense = 5f;
    public float moveSpeed   = 5f;

    [Header("공격 구성")]
    [Tooltip("기본 공격 데이터 (AttackDataSO)")]
    public AttackDataSO basicAttack;
    [Tooltip("스킬 공격 데이터 (AttackDataSO) — 근접이어도 됨, 투사체어도 됨")]
    public AttackDataSO skillAttack;

    [Header("강화 배율 (레벨당 증가율)")]
    public float attackUpgradeBonus  = 0.10f;   // 10%
    public float hpUpgradeBonus      = 0.08f;   // 8%
    public float defenseUpgradeBonus = 0.05f;   // 5%
}
