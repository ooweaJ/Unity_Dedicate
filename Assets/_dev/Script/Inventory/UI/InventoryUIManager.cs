using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct TabPanelMapping
{
    public int tabId;
    public List<GameObject> panelsToShow; // 켜질 것만 관리
}

public class InventoryUIManager : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [Header("Sub Managers")]
    [SerializeField] private CharacterListManager listManager;   // 오른쪽 리스트 관리
    [SerializeField] private CharacterModelManager modelManager; // 중앙 3D 모델 관리
    [SerializeField] private CharacterInfoPanel infoPanel;       // 왼쪽 스탯/상세정보 관리
    [SerializeField] private ItemInfoPanel itemInfoPanel;           // 왼쪽 아이템 상세정보 관리
    [SerializeField] private SidebarManager sidebarManager;     // 왼쪽 탭 관리

    [Header("Item Tab UIs")]
    [SerializeField] private List<InventoryUI> _itemInventoryUIs;

    [Header("Tab Panels")]
    [SerializeField] private List<TabPanelMapping> panelMappings;

    private List<CharacterUIModel> _characterUIModels = new();
    private Dictionary<int, TabPanelMapping> _panelDict = new();

    // ✅ 캐시된 데이터들
    private List<PlayerItemData> _cachedItemDatas = new();
    private List<PlayerCharacterData> _cachedCharacterDatas = new();
    private int _selectedCharacterId = -1;

    private void OnEnable()
    {
        foreach (var mapping in panelMappings)
            _panelDict[mapping.tabId] = mapping;

        // ✅ 각 아이템 UI에 데이터 공급 방식 등록 (캐릭터 데이터 및 선택된 ID 포함)
        foreach (var inventoryUI in _itemInventoryUIs)
        {
            inventoryUI.Setup(
                () => _cachedItemDatas, 
                () => _cachedCharacterDatas,
                () => _selectedCharacterId,
                OnSelectItem,
                OnItemHoverEnter,
                OnItemHoverExit
            );
        }

        sidebarManager.OnTabChanged += SwitchSubPanel;
        sidebarManager.Init();
    }

    private void OnDisable()
    {
        sidebarManager.OnTabChanged -= SwitchSubPanel;
    }

    public void Open() { panel.SetActive(true); }
    public void Close() { panel.SetActive(false); modelManager.ClearAllModels(); itemInfoPanel.Hide(); }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void InitInventory(IEnumerable<PlayerCharacterData> charDatas, IEnumerable<PlayerItemData> itemDatas)
    {
        // 1. 데이터 캐시 업데이트
        _cachedItemDatas = itemDatas != null ? itemDatas.ToList() : new List<PlayerItemData>();
        _cachedCharacterDatas = charDatas != null ? charDatas.ToList() : new List<PlayerCharacterData>();

        // 2. 캐릭터 리스트 초기화
        _characterUIModels = _cachedCharacterDatas.Select(s =>
            new CharacterUIModel(s, GameDataManager.Instance.GetCharacter(s.characterId))
        ).ToList();

        listManager.Init(_characterUIModels, 1, OnSelectCharacter);

        if (_characterUIModels.Count > 0)
        {
            OnSelectCharacter(_characterUIModels[0].StaticData.id);
        }

        // 3. 현재 활성화된 아이템 탭 즉시 갱신
        RefreshActiveItemUI();
    }

    private void RefreshActiveItemUI()
    {
        foreach (var inventoryUI in _itemInventoryUIs)
        {
            if (inventoryUI.gameObject.activeInHierarchy)
            {
                inventoryUI.Refresh();
            }
        }
    }

    private void SwitchSubPanel(int tabId)
    {
        foreach (var mapping in _panelDict.Values)
            mapping.panelsToShow.ForEach(p => p.SetActive(false));

        if (_panelDict.TryGetValue(tabId, out var target))
            target.panelsToShow.ForEach(p => p.SetActive(true));

        // ✅ 탭 전환 시 즉시 숨김 (딜레이 없이)
        itemInfoPanel.Hide();
    }

    private void OnItemHoverExit()
    {
        // ✅ 딜레이 숨김으로 변경 (즉시 숨기면 재진입 시 깜빡임 발생 가능)
        itemInfoPanel.Hide();
    }

    private void OnSelectItem(int itemId)
    {
        // 클릭 시 로직 (필요 시 유지)
    }

    private void OnItemHoverEnter(int itemId, Vector2 position)
    {
        // 1. 데이터 찾기 (OnSelectItem 로직 재활용)
        var itemData = _cachedItemDatas.FirstOrDefault(i => i.itemId == itemId);
        if (itemData != null)
        {
            var staticData = GameDataManager.Instance.GetItem(itemId);
            itemInfoPanel.SetData(staticData, itemData.amount);
            itemInfoPanel.ShowAt(position);
            return;
        }

        var charData = _cachedCharacterDatas.FirstOrDefault(c => c.characterId == itemId);
        if (charData != null)
        {
            var staticData = GameDataManager.Instance.GetCharacter(itemId);
            itemInfoPanel.SetShardData(staticData, charData.shardAmount);
            itemInfoPanel.ShowAt(position);
        }
    }

    private void OnSelectCharacter(int id)
    {
        _selectedCharacterId = id; // ✅ 현재 선택된 캐릭터 ID 업데이트

        var selectedModel = _characterUIModels.FirstOrDefault(m => m.StaticData.id == id);
        if (selectedModel == null) return;

        modelManager.ShowModel(selectedModel);
        infoPanel.SetData(selectedModel);
        listManager.RefreshSelection(id);

        // ✅ 캐릭터가 바뀌었으므로 현재 열려있는 아이템 탭(특히 초월 탭)도 즉시 갱신
        RefreshActiveItemUI();
    }
}