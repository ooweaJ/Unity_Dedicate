using Mirror;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private PlayerInfoUI playerInfoUI;
    [SerializeField] private InventoryUI inventoryUI;

    private void Awake()
    {
        Instance = this;
        playerInfoUI.OnLogoutButtonClicked += HandleLogout;
        lobbyUI.OnInventoryButtonClicked += HandleOnInventory;
        lobbyUI.OnMatchButtonClicked += HandleOnMatch;
        lobbyUI.OnStoreButtonClicked += HandleOnStore;
    }

    private void OnDestroy()
    {
        playerInfoUI.OnLogoutButtonClicked -= HandleLogout;
        lobbyUI.OnInventoryButtonClicked -= HandleOnInventory;
        lobbyUI.OnMatchButtonClicked -= HandleOnMatch;
        lobbyUI.OnStoreButtonClicked -= HandleOnStore;
    }
    private void OnEnable()
    {
        UpdateInfoUI();
    }

    private void UpdateInfoUI()
    {
        if (PlayerDataManager.Instance)
        {
            playerInfoUI.UpdateUI(
                PlayerDataManager.Instance.GetUsername(),
                PlayerDataManager.Instance.GetLevel(),
                PlayerDataManager.Instance.GetGold()
                );
        }
    }

    public void HandleLogout()
    {
        // 플레이어 데이터 초기화
        PlayerDataManager.Instance.ClearData();

        if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }

        // 씬 이동
        SceneManager.LoadScene("LoginScene");
    }

    private void HandleOnStore()
    {

    }

    private void HandleOnInventory() { inventoryUI.Open(); }
    private void HandleOffInventory() { inventoryUI.Close(); }

    public async void HandleDraw()
    {
        int userId = PlayerDataManager.Instance.GetUserId();

        string res = await BackendManager.DrawGacha(userId);
        Debug.Log("Gacha Response Raw: " + res);

        JObject json = JObject.Parse(res);
    }

    void HandleOnMatch()
    {
        if (LobbyNetworkPlayer.Local == null)
        {
            Debug.LogError("Local Network Player not found");
            return;
        }

        LobbyNetworkPlayer.Local.CmdRequestMatch();
    }
}
