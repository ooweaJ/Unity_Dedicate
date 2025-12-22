using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private PlayerInfoUI playerInfoUI;
    [SerializeField] private InventoryUI inventoryUI;

    private void Awake()
    {
        playerInfoUI.OnLogoutButtonClicked += HandleLogout;
        lobbyUI.OnInventoryButtonClicked += HandleOnInventory;
    }

    private void OnDestroy()
    {
        playerInfoUI.OnLogoutButtonClicked -= HandleLogout;
        lobbyUI.OnInventoryButtonClicked -= HandleOnInventory;
    }

    private void OnEnable()
    {
        UpdateInfoUI();
    }

    private void UpdateInfoUI()
    {
        playerInfoUI.UpdateUI(
            PlayerDataManager.Instance.GetUsername(),
            PlayerDataManager.Instance.GetLevel(),
            PlayerDataManager.Instance.GetGold()
            );
    }

    private void HandleLogout()
    {
        // 플레이어 데이터 초기화
        PlayerDataManager.Instance.ClearData();
        // 씬 이동
        SceneManager.LoadScene("LoginScene");
    }

    private void HandleOnInventory() { inventoryUI.Open(); }
    private void HandleOffInventory() { inventoryUI.Close(); }

    public async void HandleDraw()
    {
        int userId = PlayerDataManager.Instance.GetUserId();

        string res = await BackendManager.DrawGacha(userId);
        Debug.Log("Gacha Response Raw: " + res);

        JObject json = JObject.Parse(res);

        //if (json["success"].ToObject<bool>())
        //{
        //    int characterId = json["characterId"].ToObject<int>();

        //    // 골드 UI 업데이트
        //    PlayerDataManager.Instance.gold -= 100;
        //    goldText.text = PlayerDataManager.Instance.gold.ToString();

        //    var newChar = new PlayerCharacterData
        //    {
        //        characterId = characterId,
        //        level = 1,
        //        exp = 0,
        //        enhance = 0
        //    };

        //    PlayerDataManager.Instance.inventory.AddOrUpdate(newChar);
        //}

    }

}
