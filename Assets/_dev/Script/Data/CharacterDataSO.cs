using UnityEngine;

/// <summary>
/// 캐릭터 고유 전투 데이터 — 외형, 이동, 공격 3슬롯
/// Project 우클릭 → Create → Game/Character Data
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("외형")]
    [Tooltip("WeaponRoot에 붙일 무기 메시 프리팹")]
    public GameObject weaponMeshPrefab;

    [Header("이동")]
    public float moveSpeed = 5f;

    [Header("공격")]
    public AttackData basicAttack;
    public AttackData skill1Attack;
    public AttackData skill2Attack;
}
