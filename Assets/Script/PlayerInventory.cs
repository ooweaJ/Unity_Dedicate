using System.Collections.Generic;

public class PlayerInventory
{
    private Dictionary<int, PlayerCharacterData> characters
        = new Dictionary<int, PlayerCharacterData>();

    public void AddOrUpdate(PlayerCharacterData data)
    {
        characters[data.characterId] = data;
    }

    public PlayerCharacterData Get(int characterId)
    {
        if (characters.TryGetValue(characterId, out var data))
            return data;

        return null;
    }

    public IEnumerable<PlayerCharacterData> GetAll()
    {
        return characters.Values;
    }
}
