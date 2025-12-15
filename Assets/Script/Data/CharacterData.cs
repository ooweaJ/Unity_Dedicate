using UnityEngine;

[System.Serializable]
public class CharacterData
{
    public int id;            // 서버에서 주는 character_id
    public string name;       // 캐릭터 이름
    public int rarity;        // 등급 (1~5)
    public int baseAtk;       // 기본 공격력
    public int baseHp;        // 기본 체력
}
