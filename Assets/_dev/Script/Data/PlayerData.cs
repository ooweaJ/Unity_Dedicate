using Newtonsoft.Json.Linq;

[System.Serializable]
public class PlayerCharacterData
{
    public int characterId;
    public int level;
    public int exp;
    public int enhance;
    public int shardAmount;
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
    public int userId { get; private set; }
    public string username { get; private set; }
    public int gold { get; private set; }
    public int level { get; private set; }

    // 데이터 원본 저장소
    public PlayerInventory inventory { get; } = new PlayerInventory();

    public void Apply(JToken user)
    {
        if (user == null) return;

        // 1. 기본 정보
        userId = (int)user["id"];
        username = (string)user["username"];
        level = (int)user["level"];
        gold = (int)user["gold"];

        // 2. 인벤토리에 데이터 배분 위임
        if (user["characters"] is JArray charArray)
            inventory.ApplyCharacters(charArray);

        if (user["items"] is JArray itemArray)
            inventory.ApplyItems(itemArray);
    }

    public void UpdateGold(int newGold)
    {
        this.gold = newGold;
        // 필요 시 여기서도 OnChanged 이벤트를 발생시킬 수 있습니다.
    }

    public void ConsumeGold(int amount)
    {
        this.gold -= amount;
    }

    public void Clear()
    {
        userId = 0;
        username = "";
        gold = 0;
        level = 0;
        inventory.Clear();
    }
}