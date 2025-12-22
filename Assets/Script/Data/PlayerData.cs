using Newtonsoft.Json.Linq;

[System.Serializable]
public class PlayerData
{
    public int userId { get; private set; }
    public string username { get; private set; }
    public int gold { get; private set; }
    public int level { get; private set; }

    public PlayerInventory inventory { get; } = new PlayerInventory();

    public void Apply(JToken user)
    {
        userId = (int)user["id"];
        username = (string)user["username"];
        level = (int)user["level"];
        gold = (int)user["gold"];
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
