using UnityEngine;

/// <summary>
/// 캐릭터 고유 데이터 ScriptableObject
/// 캐릭터마다 하나씩 에셋 파일 생성
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string characterName = "캐릭터";
    public Sprite characterIcon;
    public GameObject characterPrefab;        // 스폰할 플레이어 프리팹

    [Header("기본 스탯")]
    public float baseAttack = 20f;
    public float baseMaxHp = 100f;
    public float baseDefense = 5f;
    public float moveSpeed = 5f;

    [Header("무기")]
    public WeaponDataSO primaryWeapon;        // 기본 공격
    public WeaponDataSO skillWeapon;          // 스킬 공격

    [Header("강화 배율")]
    public float attackUpgradeBonus = 0.10f; // 레벨당 10%
    public float hpUpgradeBonus = 0.08f;
    public float defenseUpgradeBonus = 0.05f;
}