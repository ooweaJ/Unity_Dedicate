using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    // 이제 UI가 아니라 매니저를 참조합니다.
    [SerializeField] private InventoryUIManager uiManager;
    [SerializeField] private LobbyUI lobbyUI;

    private PlayerInventory _inventory;

    // UI는 껐다 켰다 하는 경우가 많으므로 OnEnable/OnDisable이 더 안전합니다.
    private void OnEnable()
    {
        if (PlayerDataManager.Instance == null) return;
        if (lobbyUI != null)
            lobbyUI.OnInventoryButtonClicked += OpenInventory;

        _inventory = PlayerDataManager.Instance.GetInventory();

        if (_inventory != null)
        {
            _inventory.OnChanged -= HandleRefresh;
            _inventory.OnChanged += HandleRefresh;

            // 처음 열렸을 때 UI 초기화 명령
            RefreshUI();
        }
    }

    private void OnDisable()
    {
        if (_inventory != null)
            _inventory.OnChanged -= HandleRefresh;
        if(lobbyUI != null)
            lobbyUI.OnInventoryButtonClicked -= OpenInventory;

    }

    public void OpenInventory()
    {
        if (uiManager == null) return;

        // 1. UI를 먼저 켭니다. (이때 UI의 OnEnable 등이 실행됨)
        uiManager.gameObject.SetActive(true);

        // 2. 데이터를 밀어넣습니다.
        RefreshUI();
    }

    private void HandleRefresh()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_inventory == null || uiManager == null) return;

        // 서버에서 온 가공되지 않은 데이터를 매니저에게 던져줍니다.
        var characters = _inventory.GetAllCharacters();
        var items = _inventory.GetAllItems();

        uiManager.InitInventory(characters, items);
    }
}