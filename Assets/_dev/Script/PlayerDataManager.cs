using Newtonsoft.Json.Linq;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    private PlayerData Data = new PlayerData();

    // 로비에서 선택한 캐릭터 — 배틀 서버 Auth 시 전달
    private CharacterType selectedCharacter = CharacterType.Swordsman;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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

    // ─── 캐릭터 선택 ──────────────────────────────────────────────────
    public void SelectCharacter(CharacterType type)
    {
        selectedCharacter = type;
        Debug.Log($"[PLAYER DATA] 캐릭터 선택: {type}");
    }

    public CharacterType GetSelectedCharacter() => selectedCharacter;

    // ─── 기본 정보 ────────────────────────────────────────────────────
    public int    GetUserId()    => Data.userId;
    public string GetUsername()  => Data.username;
    public int    GetLevel()     => Data.level;
    public int    GetGold()      => Data.gold;
    public PlayerInventory GetPlayerInventory() => Data.inventory;
}
