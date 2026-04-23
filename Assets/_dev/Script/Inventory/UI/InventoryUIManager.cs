using System;
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
    [SerializeField] private CharacterListManager listManager;
    [SerializeField] private CharacterModelManager modelManager;
    [SerializeField] private CharacterInfoPanel infoPanel;
    [SerializeField] private ItemInfoPanel itemInfoPanel;
    [SerializeField] private SidebarManager sidebarManager;

    [Header("Item UIs & Popups")]
    [SerializeField] private List<InventoryUI> _itemInventoryUIs;
    [SerializeField] private List<EquipmentSlot> _equipmentSlots;
    [SerializeField] private ItemActionPopup actionPopup;

    [Header("Tab Panels")]
    [SerializeField] private List<TabPanelMapping> panelMappings;

    private List<CharacterUIModel> _characterUIModels = new();
    private Dictionary<int, TabPanelMapping> _panelDict = new();

    // 데이터 캐시
    private List<PlayerItemData> _cachedItemDatas = new();
    private List<PlayerCharacterData> _cachedCharacterDatas = new();
    private int _selectedCharacterId = -1;

    // 외부 게임 로직용 이벤트
    public event Action<int> OnUseItem;
    public event Action<int> OnDiscardItem;
    public event Action<int> OnOpenTranscendence;
    public event Action<int, int, EquipmentSlotType> OnEquipItem;   // (charId, itemId, slot)
    public event Action<int, EquipmentSlotType> OnUnequipItem;       // (charId, slot)

    private void OnEnable()
    {
        foreach (var mapping in panelMappings)
            _panelDict[mapping.tabId] = mapping;

        // 아이템 인벤토리 UI 이벤트 구독
        foreach (var inventoryUI in _itemInventoryUIs)
        {
            inventoryUI.Setup(
                () => _cachedItemDatas, 
                () => _cachedCharacterDatas,
                () => _selectedCharacterId
            );
            
            inventoryUI.OnItemClicked += HandleItemClicked;
            inventoryUI.OnItemHoverEnter += HandleItemHoverEnter;
            inventoryUI.OnItemHoverExit += HandleItemHoverExit;
            inventoryUI.OnItemDragBegin += HandleItemDragBegin;
        }

        // 장비 슬롯 이벤트 구독
        foreach (var slot in _equipmentSlots)
        {
            slot.OnItemDropped += HandleEquip;
            slot.OnItemClicked += HandleUnequip;
        }

        sidebarManager.OnTabChanged += SwitchSubPanel;
        sidebarManager.Init();
    }

    private void OnDisable()
    {
        foreach (var inventoryUI in _itemInventoryUIs)
        {
            inventoryUI.OnItemClicked -= HandleItemClicked;
            inventoryUI.OnItemHoverEnter -= HandleItemHoverEnter;
            inventoryUI.OnItemHoverExit -= HandleItemHoverExit;
            inventoryUI.OnItemDragBegin -= HandleItemDragBegin;
        }
        
        foreach (var slot in _equipmentSlots)
        {
            slot.OnItemDropped -= HandleEquip;
            slot.OnItemClicked -= HandleUnequip;
        }

        sidebarManager.OnTabChanged -= SwitchSubPanel;
    }

    public void Open() { panel.SetActive(true); }
    public void Close() 
    { 
        panel.SetActive(false); 
        modelManager.ClearAllModels(); 
        itemInfoPanel.Hide(); 
        actionPopup?.Hide();
    }

    public void InitInventory(IEnumerable<PlayerCharacterData> charDatas, IEnumerable<PlayerItemData> itemDatas)
    {
        _cachedItemDatas = itemDatas != null ? itemDatas.ToList() : new List<PlayerItemData>();
        _cachedCharacterDatas = charDatas != null ? charDatas.ToList() : new List<PlayerCharacterData>();

        _characterUIModels = _cachedCharacterDatas.Select(s =>
            new CharacterUIModel(s, GameDataManager.Instance.GetCharacter(s.characterId))
        ).ToList();

        listManager.Init(_characterUIModels, 1, OnSelectCharacter);

        // 이전에 선택한 캐릭터가 있으면 유지, 없으면 첫 번째 캐릭터 선택
        bool keepSelection = _selectedCharacterId != -1 &&
            _characterUIModels.Any(m => m.StaticData.id == _selectedCharacterId);

        int idToSelect = keepSelection
            ? _selectedCharacterId
            : (_characterUIModels.Count > 0 ? _characterUIModels[0].StaticData.id : -1);

        if (idToSelect != -1)
            OnSelectCharacter(idToSelect);

        RefreshActiveItemUI();
    }

    private void RefreshActiveItemUI()
    {
        foreach (var inventoryUI in _itemInventoryUIs)
        {
            if (inventoryUI.gameObject.activeInHierarchy)
                inventoryUI.Refresh();
        }
    }

    private void SwitchSubPanel(int tabId)
    {
        foreach (var mapping in _panelDict.Values)
            mapping.panelsToShow.ForEach(p => p.SetActive(false));

        if (_panelDict.TryGetValue(tabId, out var target))
            target.panelsToShow.ForEach(p => p.SetActive(true));

        itemInfoPanel.Hide();
        actionPopup.Hide();
    }

    // --- 슬롯 이벤트 핸들러 ---

    private void HandleItemHoverEnter(int itemId, Vector2 pos)
    {
        var itemData = _cachedItemDatas.FirstOrDefault(i => i.itemId == itemId);
        if (itemData != null)
        {
            var staticData = GameDataManager.Instance.GetItem(itemId);
            itemInfoPanel.SetData(staticData, itemData.amount);
            itemInfoPanel.ShowAt(pos);
            return;
        }

        var charData = _cachedCharacterDatas.FirstOrDefault(c => c.characterId == itemId);
        if (charData != null)
        {
            var staticData = GameDataManager.Instance.GetCharacter(itemId);
            itemInfoPanel.SetShardData(staticData, charData.shardAmount);
            itemInfoPanel.ShowAt(pos);
        }
    }

    private void HandleItemHoverExit() => itemInfoPanel.Hide();

    private void HandleItemClicked(int itemId, Vector2 pos)
    {
        var data = GameDataManager.Instance.GetItem(itemId);
        if (data == null) return;

        // 소비템/재료는 액션 팝업 표시
        if (data.itemType != ItemType.Equipment)
        {
            actionPopup.Show(data, pos, actionType => HandleAction(itemId, actionType));
        }
        
        // 선택 하이라이트 업데이트
        foreach (var ui in _itemInventoryUIs) ui.RefreshSelection(itemId);
    }

    private void HandleItemDragBegin(int itemId)
    {
        var data = GameDataManager.Instance.GetItem(itemId);
        if (data != null && DragController.Instance != null)
        {
            DragController.Instance.BeginDrag(data);
        }
        itemInfoPanel.Hide();
        actionPopup.Hide();
    }

    private void HandleAction(int itemId, ItemActionType actionType)
    {
        switch (actionType)
        {
            case ItemActionType.Use:               OnUseItem?.Invoke(itemId);            break;
            case ItemActionType.Discard:           OnDiscardItem?.Invoke(itemId);        break;
            case ItemActionType.OpenTranscendence: OnOpenTranscendence?.Invoke(itemId);  break;
        }
    }

    private void HandleEquip(int itemId, EquipmentSlotType slotType)
        => OnEquipItem?.Invoke(_selectedCharacterId, itemId, slotType);

    private void HandleUnequip(int itemId)
    {
        var slot = _equipmentSlots.FirstOrDefault(s => s.ItemId == itemId);
        if (slot != null)
            OnUnequipItem?.Invoke(_selectedCharacterId, slot.acceptedSlotType);
    }

    private void RefreshEquipmentSlots(PlayerCharacterData charData)
    {
        foreach (var slot in _equipmentSlots)
        {
            if (charData.equippedItems.TryGetValue(slot.acceptedSlotType, out int itemId))
            {
                var staticData = GameDataManager.Instance.GetItem(itemId);
                if (staticData == null)
                {
                    Debug.LogWarning($"[InventoryUIManager] 장착 아이템 ID {itemId}가 ItemTableSO에 없습니다.");
                    slot.Clear();
                    continue;
                }
                slot.SetItem(itemId, staticData, 1);
            }
            else
            {
                slot.Clear();
            }
        }
    }

    private void OnSelectCharacter(int id)
    {
        _selectedCharacterId = id;
        var selectedModel = _characterUIModels.FirstOrDefault(m => m.StaticData.id == id);
        if (selectedModel == null) return;

        modelManager.ShowModel(selectedModel);
        infoPanel.SetData(selectedModel);
        listManager.RefreshSelection(id);
        RefreshEquipmentSlots(selectedModel.ServerData);
        RefreshActiveItemUI();
    }
}
