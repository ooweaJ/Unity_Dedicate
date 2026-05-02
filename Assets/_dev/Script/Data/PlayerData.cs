using System.Collections.Generic;
using Newtonsoft.Json.Linq;

// 장비 인스턴스 (user_items_equipment 1행 = 강화 수치가 붙은 개별 장비)
[System.Serializable]
public class PlayerEquipmentData
{
    public int equip_instance_id; // user_items_equipment.id
    public int itemId;            // game_items.item_id (종류 조회용)
    public int enhance;           // 현재 강화 단계 0~8
}

[System.Serializable]
public class PlayerCharacterData
{
    public int characterId;
    public int level;
    public int exp;
    public int enhance;      // 초월 단계 0~5
    public int shardAmount;
    // 슬롯별 장착 장비 인스턴스 (equip_instance_id 참조)
    public Dictionary<EquipmentSlotType, PlayerEquipmentData> equippedItems = new();
}

[System.Serializable]
public class PlayerItemData
{
    public int itemId;
    public int amount;
}

[System.Serializable]
public class PlayerData
{
    public int    userId              { get; private set; }
    public string username            { get; private set; }
    public int    level               { get; private set; }
    public int    exp                 { get; private set; }
    public int    gold                { get; private set; }
    public int    selectedCharacterId { get; private set; } = -1;

    public PlayerInventory inventory { get; } = new PlayerInventory();

    public void Apply(JToken user)
    {
        if (user == null) return;

        userId   = (int)user["id"];
        username = (string)user["username"];
        level    = (int)user["level"];
        exp      = user["exp"] != null ? (int)user["exp"] : 0;
        gold     = (int)user["gold"];

        var selToken = user["selected_character_id"];
        selectedCharacterId = (selToken == null || selToken.Type == JTokenType.Null)
            ? -1 : (int)selToken;

        if (user["characters"] is JArray charArray)
            inventory.ApplyCharacters(charArray);

        if (user["equipment"] is JArray equipArray)
            inventory.ApplyEquipment(equipArray);

        if (user["items"] is JArray itemArray)
            inventory.ApplyItems(itemArray);
    }

    public void ConsumeGold(int amount) => gold -= amount;

    public void Clear()
    {
        userId              = 0;
        username            = "";
        level               = 0;
        exp                 = 0;
        gold                = 0;
        selectedCharacterId = -1;
        inventory.Clear();
    }
}
