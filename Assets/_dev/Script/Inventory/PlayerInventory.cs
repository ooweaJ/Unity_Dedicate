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
                level       = (int)token["level"],
                exp         = (int)token["exp"],
                enhance     = (int)token["enhance"],
                shardAmount = (int)token["shardAmount"]
            };

            // 서버에서 내려온 장착 장비 파싱
            if (token["equipped_items"] is JArray equippedArray)
            {
                foreach (var eq in equippedArray)
                {
                    if (System.Enum.TryParse<EquipmentSlotType>((string)eq["slot_type"], out var slotType))
                        data.equippedItems[slotType] = (int)eq["item_id"];
                }
            }

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
                amount = (int)token["amount"]
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

    public void EquipItem(int characterId, int itemId, EquipmentSlotType slot)
    {
        if (!characters.TryGetValue(characterId, out var charData)) return;

        // 같은 슬롯에 이미 장착된 아이템이 있으면 인벤토리로 반환
        if (charData.equippedItems.TryGetValue(slot, out int existingItemId))
            ReturnItemToInventory(existingItemId);

        charData.equippedItems[slot] = itemId;
        RemoveItemFromInventory(itemId);

        OnChanged?.Invoke();

        // TODO: 서버 동기화 → BackendManager.EquipItem(characterId, itemId, slot)
    }

    public void UnequipItem(int characterId, EquipmentSlotType slot)
    {
        if (!characters.TryGetValue(characterId, out var charData)) return;
        if (!charData.equippedItems.TryGetValue(slot, out int itemId)) return;

        charData.equippedItems.Remove(slot);
        ReturnItemToInventory(itemId);

        OnChanged?.Invoke();

        // TODO: 서버 동기화 → BackendManager.UnequipItem(characterId, slot)
    }

    private void RemoveItemFromInventory(int itemId)
    {
        if (!items.TryGetValue(itemId, out var itemData)) return;
        if (itemData.amount > 1)
            itemData.amount--;
        else
            items.Remove(itemId);
    }

    private void ReturnItemToInventory(int itemId)
    {
        if (items.TryGetValue(itemId, out var existing))
            existing.amount++;
        else
            items[itemId] = new PlayerItemData { itemId = itemId, amount = 1 };
    }

    public void Clear()
    {
        characters.Clear();
        items.Clear();
        OnChanged?.Invoke();
    }
}