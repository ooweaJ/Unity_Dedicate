using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class PlayerInventory
{
    private Dictionary<int, PlayerCharacterData>  characters = new();
    private Dictionary<int, PlayerEquipmentData>  equipment  = new(); // equip_instance_id → data
    private Dictionary<int, PlayerItemData>       items      = new();

    public event Action OnChanged;

    // ── Apply (서버 응답 파싱) ─────────────────────────────────────────

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

            if (token["equipped_items"] is JArray equippedArray)
            {
                foreach (var eq in equippedArray)
                {
                    if (!Enum.TryParse<EquipmentSlotType>((string)eq["slot_type"], true, out var slotType))
                        continue;

                    data.equippedItems[slotType] = new PlayerEquipmentData
                    {
                        equip_instance_id = (int)eq["equip_instance_id"],
                        itemId            = (int)eq["item_id"],
                        enhance           = (int)eq["enhance"]
                    };
                }
            }

            characters[data.characterId] = data;
        }
        OnChanged?.Invoke();
    }

    public void ApplyEquipment(JArray array)
    {
        equipment.Clear();
        if (array == null) return;

        foreach (var token in array)
        {
            var data = new PlayerEquipmentData
            {
                equip_instance_id = (int)token["equip_instance_id"],
                itemId            = (int)token["item_id"],
                enhance           = (int)token["enhance"]
            };
            equipment[data.equip_instance_id] = data;
        }
        OnChanged?.Invoke();
    }

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

    // ── 장비 장착 / 해제 (낙관적 업데이트용) ─────────────────────────

    public void EquipItem(int characterId, PlayerEquipmentData equipData, EquipmentSlotType slot)
    {
        if (!characters.TryGetValue(characterId, out var charData)) return;
        charData.equippedItems[slot] = equipData;
        OnChanged?.Invoke();
    }

    public void UnequipItem(int characterId, EquipmentSlotType slot)
    {
        if (!characters.TryGetValue(characterId, out var charData)) return;
        charData.equippedItems.Remove(slot);
        OnChanged?.Invoke();
    }

    // ── 캐릭터 수정 ───────────────────────────────────────────────────

    public void AddOrUpdateCharacter(PlayerCharacterData data)
    {
        characters[data.characterId] = data;
        OnChanged?.Invoke();
    }

    // ── Getter ────────────────────────────────────────────────────────

    public PlayerCharacterData  GetCharacter(int id)        => characters.GetValueOrDefault(id);
    public PlayerEquipmentData  GetEquipment(int instanceId) => equipment.GetValueOrDefault(instanceId);
    public PlayerItemData       GetItem(int id)              => items.GetValueOrDefault(id);

    public IEnumerable<PlayerCharacterData> GetAllCharacters() => characters.Values;
    public IEnumerable<PlayerEquipmentData> GetAllEquipment()  => equipment.Values;
    public IEnumerable<PlayerItemData>      GetAllItems()      => items.Values;

    public void Clear()
    {
        characters.Clear();
        equipment.Clear();
        items.Clear();
        OnChanged?.Invoke();
    }
}
