using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class PlayerInventory
{
    private Dictionary<int, PlayerCharacterData> characters = new();
    private Dictionary<int, PlayerItemData> items = new();

    public event Action OnChanged;

    // 캐릭터 데이터 적용
    public void ApplyCharacters(JArray charArray)
    {
        characters.Clear();
        if (charArray == null) return;

        foreach (var token in charArray)
        {
            var data = new PlayerCharacterData
            {
                characterId = (int)token["character_id"],
                level = (int)token["level"],
                exp = (int)token["exp"],
                enhance = (int)token["enhance"],
                shardAmount = (int)token["shardAmount"]
            };
            characters[data.characterId] = data;
        }
        OnChanged?.Invoke();
    }

    // 아이템 데이터 적용
    public void ApplyItems(JArray itemArray)
    {
        items.Clear();
        if (itemArray == null) return;

        foreach (var token in itemArray)
        {
            var data = new PlayerItemData
            {
                itemId = (int)token["item_id"],
                amoutn = (int)token["amount"]
            };
            items[data.itemId] = data;
        }
        OnChanged?.Invoke();
    }

    // 데이터 수정/추가 시 호출
    public void AddOrUpdateCharacter(PlayerCharacterData data)
    {
        characters[data.characterId] = data;
        OnChanged?.Invoke();
    }

    // Getter (조회용)
    public PlayerCharacterData GetCharacter(int id) => characters.GetValueOrDefault(id);
    public PlayerItemData GetItem(int id) => items.GetValueOrDefault(id);

    public IEnumerable<PlayerCharacterData> GetAllCharacters() => characters.Values;
    public IEnumerable<PlayerItemData> GetAllItems() => items.Values;

    public void Clear()
    {
        characters.Clear();
        items.Clear();
        OnChanged?.Invoke();
    }
}