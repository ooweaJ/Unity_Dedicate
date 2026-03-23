using System;
using System.Collections.Generic;

public class PlayerInventory
{
    private Dictionary<int, PlayerCharacterData> characters
        = new Dictionary<int, PlayerCharacterData>();

    private Dictionary<int, int> characterShards
    = new Dictionary<int, int>();

    public event Action OnChanged;
    public void AddOrUpdate(PlayerCharacterData data)
    {
        characters[data.characterId] = data;
    }

    public void AddShard(int characterId, int amount)
    {
        if (!characterShards.ContainsKey(characterId))
            characterShards[characterId] = 0;

        characterShards[characterId] += amount;
    }

    public PlayerCharacterData Get(int characterId)
    {
        return characters.TryGetValue(characterId, out var c) ? c : null;
    }

    public IEnumerable<PlayerCharacterData> GetAll()
    {
        return characters.Values;
    }

    public void Clear()
    {
        characters.Clear();
        characterShards.Clear();

        OnChanged?.Invoke();
    }
}
