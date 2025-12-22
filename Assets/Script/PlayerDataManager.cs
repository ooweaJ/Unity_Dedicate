using Newtonsoft.Json.Linq;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    private PlayerData Data = new PlayerData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ApplyUserData(JToken data)
    {
        Data.Apply(data);
    }
    public void ClearData()
    {
        Data.Clear();
    }

    public void CharacterAddOrUpdate(PlayerCharacterData data)
    {
        Data.inventory.AddOrUpdate(data);
    }

    public int GetUserId() => Data.userId;
    public string GetUsername() => Data.username;
    public int GetLevel() => Data.level;
    public int GetGold() => Data.gold;
    public PlayerInventory GetPlayerInventory() => Data.inventory;
}
