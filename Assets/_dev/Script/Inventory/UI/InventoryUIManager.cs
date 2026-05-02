using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct TabPanelMapping
{
    public int tabId;
    public List<GameObject> panelsToShow;
}

public class InventoryUIManager : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [Header("Sub Managers")]
    [SerializeField] private CharacterListManager  listManager;
    [SerializeField] private CharacterModelManager modelManager;
    [SerializeField] private List<CharacterInfoPanel> CharacterinfoPanels;
    [SerializeField] private ItemInfoPanel         itemInfoPanel;
    [SerializeField] private SidebarManager        sidebarManager;

    [Header("Item UIs & Popups")]
    [SerializeField] private List<InventoryUI>    _itemInventoryUIs;
    [SerializeField] private List<EquipmentSlot>  _equipmentSlots;
    [SerializeField] private ItemActionPopup       actionPopup;

    [Header("Tab Panels")]
    [SerializeField] private List<TabPanelMapping> panelMappings;

    private List<CharacterUIModel>    _characterUIModels   = new();
    private Dictionary<int, TabPanelMapping> _panelDict   = new();

    // 데이터 캐시
    private List<PlayerCharacterData>  _cachedCharacterDatas  = new();
    private List<PlayerEquipmentData>  _cachedEquipmentDatas  = new();
    private List<PlayerItemData>       _cachedItemDatas       = new();
    private int _selectedCharacterId = -1;

    // ── 이벤트 (InventoryController가 구독) ──────────────────────────
    public event Action<int, int>                OnUseItem;         // (charId, itemId)
    public event Action<int>                     OnDiscardItem;
    public event Action<int>                     OnOpenTranscendence; // (charId)
    public event Action<int, int, EquipmentSlotType> OnEquipItem;   // (charId, equipInstanceId, slot)
    public event Action<int, EquipmentSlotType>  OnUnequipItem;     // (charId, slot)
    public event Action<int>                     OnEnhanceEquipment; // (equipInstanceId)

    private void OnEnable()
    {
        foreach (var mapping in panelMappings)
            _panelDict[mapping.tabId] = mapping;

        foreach (var inventoryUI in _itemInventoryUIs)
        {
            inventoryUI.Setup(
                () => _cachedItemDatas,
                () => _cachedEquipmentDatas,
                () => _cachedCharacterDatas,
                () => _selectedCharacterId
            );

            inventoryUI.OnItemClicked    += HandleItemClicked;
            inventoryUI.OnItemHoverEnter += HandleItemHoverEnter;
            inventoryUI.OnItemHoverExit  += HandleItemHoverExit;
            inventoryUI.OnItemDragBegin  += HandleItemDragBegin;
        }

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
            inventoryUI.OnItemClicked    -= HandleItemClicked;
            inventoryUI.OnItemHoverEnter -= HandleItemHoverEnter;
            inventoryUI.OnItemHoverExit  -= HandleItemHoverExit;
            inventoryUI.OnItemDragBegin  -= HandleItemDragBegin;
        }

        foreach (var slot in _equipmentSlots)
        {
            slot.OnItemDropped -= HandleEquip;
            slot.OnItemClicked -= HandleUnequip;
        }

        sidebarManager.OnTabChanged -= SwitchSubPanel;
    }

    public void Open()  => panel.SetActive(true);
    public void Close()
    {
        panel.SetActive(false);
        modelManager.ClearAllModels();
        itemInfoPanel.Hide();
        actionPopup?.Hide();
    }

    // ── 초기화 ────────────────────────────────────────────────────────

    public void InitInventory(
        IEnumerable<PlayerCharacterData> charDatas,
        IEnumerable<PlayerItemData>      itemDatas,
        IEnumerable<PlayerEquipmentData> equipmentDatas)
    {
        _cachedCharacterDatas = charDatas      != null ? charDatas.ToList()      : new();
        _cachedItemDatas      = itemDatas       != null ? itemDatas.ToList()      : new();
        _cachedEquipmentDatas = equipmentDatas  != null ? equipmentDatas.ToList() : new();

        _characterUIModels = _cachedCharacterDatas
            .Select(s => new CharacterUIModel(s, GameDataManager.Instance.GetCharacter(s.characterId)))
            .ToList();

        bool keepSelection = _selectedCharacterId != -1 &&
            _characterUIModels.Any(m => m.StaticData.id == _selectedCharacterId);

        int idToSelect = keepSelection
            ? _selectedCharacterId
            : (_characterUIModels.Count > 0 ? _characterUIModels[0].StaticData.id : -1);

        listManager.Init(_characterUIModels, idToSelect, OnSelectCharacter);

        if (idToSelect != -1)
            OnSelectCharacter(idToSelect);

        RefreshActiveItemUI();
    }

    private void RefreshActiveItemUI()
    {
        foreach (var ui in _itemInventoryUIs)
            if (ui.gameObject.activeInHierarchy)
                ui.Refresh();
    }

    private void SwitchSubPanel(int tabId)
    {
        foreach (var mapping in _panelDict.Values)
            mapping.panelsToShow.ForEach(p => p.SetActive(false));

        if (_panelDict.TryGetValue(tabId, out var target))
            target.panelsToShow.ForEach(p => p.SetActive(true));

        itemInfoPanel.Hide();
        actionPopup?.Hide();
    }

    // ── 슬롯 이벤트 핸들러 ───────────────────────────────────────────

    private void HandleItemHoverEnter(int id, Vector2 pos)
    {
        // 장비 인스턴스 hover
        var equipData = _cachedEquipmentDatas.FirstOrDefault(e => e.equip_instance_id == id);
        if (equipData != null)
        {
            var staticData = GameDataManager.Instance.GetItem(equipData.itemId);
            if (staticData != null)
            {
                itemInfoPanel.SetData(staticData, equipData.enhance);
                itemInfoPanel.ShowAt(pos);
            }
            return;
        }

        // 소모품/재료 hover
        var itemData = _cachedItemDatas.FirstOrDefault(i => i.itemId == id);
        if (itemData != null)
        {
            var staticData = GameDataManager.Instance.GetItem(id);
            itemInfoPanel.SetData(staticData, itemData.amount);
            itemInfoPanel.ShowAt(pos);
            return;
        }

        // 초월 조각 hover
        var charData = _cachedCharacterDatas.FirstOrDefault(c => c.characterId == id);
        if (charData != null)
        {
            var staticData = GameDataManager.Instance.GetCharacter(id);
            itemInfoPanel.SetShardData(staticData, charData.shardAmount);
            itemInfoPanel.ShowAt(pos);
        }
    }

    private void HandleItemHoverExit() => itemInfoPanel.Hide();

    private void HandleItemClicked(int id, Vector2 pos)
    {
        // 장비 인스턴스 클릭 → 강화 이벤트
        var equipData = _cachedEquipmentDatas.FirstOrDefault(e => e.equip_instance_id == id);
        if (equipData != null)
        {
            OnEnhanceEquipment?.Invoke(id);
            foreach (var ui in _itemInventoryUIs) ui.RefreshSelection(id);
            return;
        }

        // 소모품/재료 클릭 → 액션 팝업
        var data = GameDataManager.Instance.GetItem(id);
        if (data == null) return;

        actionPopup.Show(data, pos, actionType => HandleAction(id, actionType));
        foreach (var ui in _itemInventoryUIs) ui.RefreshSelection(id);
    }

    private void HandleItemDragBegin(int id)
    {
        // 장비 인스턴스 드래그 → equip_instance_id + ItemRawData 전달
        var equipData = _cachedEquipmentDatas.FirstOrDefault(e => e.equip_instance_id == id);
        if (equipData != null)
        {
            var staticData = GameDataManager.Instance.GetItem(equipData.itemId);
            if (staticData != null)
                DragController.Instance?.BeginDrag(staticData, equipData.equip_instance_id);
            itemInfoPanel.Hide();
            actionPopup?.Hide();
            return;
        }

        var data = GameDataManager.Instance.GetItem(id);
        if (data != null)
            DragController.Instance?.BeginDrag(data);
        itemInfoPanel.Hide();
        actionPopup?.Hide();
    }

    private void HandleAction(int itemId, ItemActionType actionType)
    {
        switch (actionType)
        {
            case ItemActionType.Use:               OnUseItem?.Invoke(_selectedCharacterId, itemId); break;
            case ItemActionType.Discard:           OnDiscardItem?.Invoke(itemId); break;
            case ItemActionType.OpenTranscendence: OnOpenTranscendence?.Invoke(_selectedCharacterId); break;
        }
    }

    // 드롭 → 장착 (equipInstanceId)
    private void HandleEquip(int equipInstanceId, EquipmentSlotType slotType)
        => OnEquipItem?.Invoke(_selectedCharacterId, equipInstanceId, slotType);

    // 장착 슬롯 클릭 → 해제
    private void HandleUnequip(int equipInstanceId)
    {
        var slot = _equipmentSlots.FirstOrDefault(s => s.ItemId == equipInstanceId);
        if (slot != null)
            OnUnequipItem?.Invoke(_selectedCharacterId, slot.acceptedSlotType);
    }

    private void RefreshEquipmentSlots(PlayerCharacterData charData)
    {
        foreach (var slot in _equipmentSlots)
        {
            if (charData.equippedItems.TryGetValue(slot.acceptedSlotType, out var equip))
            {
                var staticData = GameDataManager.Instance.GetItem(equip.itemId);
                if (staticData == null)
                {
                    Debug.LogWarning($"[InventoryUIManager] 장착 아이템 ID {equip.itemId}가 ItemTableSO에 없습니다.");
                    slot.Clear();
                    continue;
                }
                // ItemId = equip_instance_id, amount = enhance (강화 표시용)
                slot.SetItem(equip.equip_instance_id, staticData, equip.enhance);
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

        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.SelectCharacter(selectedModel.StaticData.type);

        modelManager.ShowModel(selectedModel);
        foreach (var chinfo in CharacterinfoPanels)
            chinfo.SetData(selectedModel);
        listManager.RefreshSelection(id);
        RefreshEquipmentSlots(selectedModel.ServerData);
        RefreshActiveItemUI();
    }
}
