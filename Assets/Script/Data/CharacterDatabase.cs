using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterData> characters;

    private Dictionary<int, CharacterData> characterMap;

    private void OnEnable()
    {
        characterMap = new Dictionary<int, CharacterData>();
        foreach (var c in characters)
        {
            characterMap[c.id] = c;
        }
    }

    public CharacterData GetById(int id)
    {
        if (characterMap.TryGetValue(id, out var data))
            return data;

        Debug.LogError($"CharacterData not found: {id}");
        return null;
    }
}
