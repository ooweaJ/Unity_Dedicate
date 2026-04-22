using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    // 이제 UI가 아니라 매니저를 참조합니다.
    [SerializeField] private InventoryUIManager uiManager;
    [SerializeField] private LobbyUI lobbyUI;

    private PlayerInventory _inventory;

    // 인벤토리 컨트롤러는 씬에 항상 존재하며 데이터와 UI를 연결하는 '브릿지' 역할을 합니다.
    private void OnEnable()
    {
        if (PlayerDataManager.Instance == null) return;

        // 1. 로비 UI의 버튼 클릭 이벤트 구독
        if (lobbyUI != null)
            lobbyUI.OnInventoryButtonClicked += OpenInventory;

        // 2. 데이터 변경 이벤트 구독
        _inventory = PlayerDataManager.Instance.GetInventory();
        if (_inventory != null)
        {
            _inventory.OnChanged -= HandleRefresh;
            _inventory.OnChanged += HandleRefresh;
        }
    }

    private void OnDisable()
    {
        if (_inventory != null)
            _inventory.OnChanged -= HandleRefresh;
        
        if (lobbyUI != null)
            lobbyUI.OnInventoryButtonClicked -= OpenInventory;
    }

    /// <summary>
    /// 외부(로비 등)에서 인벤토리를 열어달라고 요청할 때 호출됩니다.
    /// </summary>
    public void OpenInventory()
    {
        if (uiManager == null) return;

        // 1. UI 패널을 먼저 엽니다.
        uiManager.Open();

        // 2. 최신 데이터를 주입하여 UI를 그립니다.
        RefreshUI();
    }

    private void HandleRefresh()
    {
        // UI가 열려 있을 때만 실시간 갱신을 수행합니다. (닫혀 있을 때는 OpenInventory 시점에 갱신됨)
        if (uiManager != null)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (_inventory == null || uiManager == null) return;

        // 인벤토리 데이터를 매니저에게 전달하여 렌더링을 지시합니다.
        var characters = _inventory.GetAllCharacters();
        var items = _inventory.GetAllItems();

        uiManager.InitInventory(characters, items);
    }
}