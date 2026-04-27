using Mirror;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private PlayerInfoUI playerInfoUI;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private ShopUI shopUI;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        playerInfoUI.OnLogoutButtonClicked += HandleLogout;
        lobbyUI.OnInventoryButtonClicked += HandleOnInventory;
        lobbyUI.OnStartMatchConfirmed += HandleStartMatch;
        lobbyUI.OnCancelMatch += HandleCancelMatch;
        lobbyUI.OnCharacterSelectedInPopup += HandlePopupCharSelect;
        lobbyUI.OnMatchButtonPressed += OpenMatchConfirmPopup;
        lobbyUI.OnStoreButtonClicked += HandleOnStore;
        ShopController.Instance.OnsuccessGacha += OnsuccessGacha;
        PlayerDataManager.Instance.OnDataUpdated += UpdateInfoUI;
        UpdateInfoUI();
    }

    private void OnDisable()
    {
        playerInfoUI.OnLogoutButtonClicked -= HandleLogout;
        lobbyUI.OnInventoryButtonClicked -= HandleOnInventory;
        lobbyUI.OnStartMatchConfirmed -= HandleStartMatch;
        lobbyUI.OnCancelMatch -= HandleCancelMatch;
        lobbyUI.OnCharacterSelectedInPopup -= HandlePopupCharSelect;
        lobbyUI.OnMatchButtonPressed -= OpenMatchConfirmPopup;
        lobbyUI.OnStoreButtonClicked -= HandleOnStore;
        ShopController.Instance.OnsuccessGacha -= OnsuccessGacha;
        PlayerDataManager.Instance.OnDataUpdated -= UpdateInfoUI;
    }

    private void OnsuccessGacha() { UpdateInfoUI(); }

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
        PlayerDataManager.Instance.ClearData();
        if (NetworkClient.isConnected) NetworkManager.singleton.StopClient();
        SceneManager.LoadScene("LoginScene");
    }

    private void HandleOnStore() { shopUI.Open(); }
    private void HandleOnInventory() { inventoryController.OpenInventory(); }

    // --- 매칭 관련 ---

    public void OpenMatchConfirmPopup()
    {
        // 팝업을 열 때 현재 보유 캐릭터들로 리스트 초기화
        var inventory = PlayerDataManager.Instance.GetInventory();
        var charModels = inventory.GetAllCharacters().Select(c => 
            new CharacterUIModel(c, GameDataManager.Instance.GetCharacter(c.characterId))
        ).ToList();

        int selectedId = -1;
        var selectedType = PlayerDataManager.Instance.GetSelectedCharacter();
        var selectedModel = charModels.FirstOrDefault(m => m.StaticData.type == selectedType);
        if (selectedModel != null) selectedId = selectedModel.CharacterId;

        lobbyUI.InitConfirmCharacterList(charModels, selectedId);
    }

    private void HandlePopupCharSelect(int charId)
    {
        var staticData = GameDataManager.Instance.GetCharacter(charId);
        if (staticData != null)
        {
            // PlayerDataManager에 선택 정보 반영
            PlayerDataManager.Instance.SelectCharacter(staticData.type);
            
            // 이미 LobbyUI의 Init 시 콜백에서 이름 갱신과 하이라이트 처리를 
            // ListManager가 내부적으로 수행하므로 여기서는 추가 작업 불필요
            Debug.Log($"캐릭터 선택됨: {staticData.displayName}");
        }
    }

    void HandleStartMatch()
    {
        if (LobbyNetworkPlayer.Local == null)
        {
            Debug.LogError("Local Network Player not found");
            lobbyUI.ShowMatchingTimer(false);
            return;
        }
        LobbyNetworkPlayer.Local.CmdRequestMatch();
    }

    void HandleCancelMatch()
    {
        Debug.Log("매칭 취소됨");
    }
}
