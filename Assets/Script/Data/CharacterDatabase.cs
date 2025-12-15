using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterData> characters;
    private Dictionary<int, CharacterData> dict;

    private void OnEnable()
    {
        dict = new Dictionary<int, CharacterData>();
        foreach (var c in characters)
            dict[c.id] = c;
    }

    // 빠르게 ID로 찾기
    public CharacterData GetById(int id)
    {
        dict.TryGetValue(id, out var data);
        return data;
    }
}
