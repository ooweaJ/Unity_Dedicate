using System;
using System.Collections.Generic;

public class PlayerInventory
{
    private Dictionary<int, PlayerCharacterData> characters
        = new Dictionary<int, PlayerCharacterData>();

    public event Action OnChanged;
    public void AddOrUpdate(PlayerCharacterData data)
    {
        characters[data.characterId] = data;
    }

    public bool HasCharacter(int characterId)
    {
        return characters.ContainsKey(characterId);
    }

    public PlayerCharacterData Get(int characterId)
    {
        return characters.TryGetValue(characterId, out var c) ? c : null;
    }

    public IEnumerable<PlayerCharacterData> GetAll()
    {
        return characters.Values;
    }
}
