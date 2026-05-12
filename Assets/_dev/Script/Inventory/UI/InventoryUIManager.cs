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
    [SerializeField] private CharacterListManager      listManager;
    [SerializeField] private CharacterModelManager     modelManager;
    [SerializeField] private List<CharacterInfoPanel>  CharacterinfoPanels;
    [SerializeField] private ItemInfoPanel             itemInfoPanel;
    [SerializeField] private SidebarManager            sidebarManager;

    [Header("Item UIs & Popups")]
    [SerializeField] private List<InventoryUI>   _itemInventoryUIs;
    [SerializeField] private List<EquipmentSlot> _equipmentSlots;
    [SerializeField] private ItemActionPopup     actionPopup;
    [SerializeField] private EnhancePanel        enhancePanel;
    [SerializeField] private TranscendPanel      transcendPanel;
    [SerializeField] private MessagePanel        messagePanel;

    [Header("Tab Panels")]
    [SerializeField] private List<TabPanelMapping> panelMappings;

    private List<CharacterUIModel>         _characterUIModels  = new();
    private Dictionary<int, TabPanelMapping> _panelDict        = new();

    private List<PlayerCharacterData>  _cachedCharacterDatas = new();
    private List<PlayerEquipmentData>  _cachedEquipmentDatas = new();
    private List<PlayerItemData>       _cachedItemDatas      = new();
    private int _selectedCharacterId = -1;

    // ── 이벤트 (InventoryController가 구독) ──────────────────────────
    public event Action<int, int>                    OnUseItem;                 // (charId, itemId)
    public event Action<int>                         OnDiscardItem;             // (itemId)
    public event Action<int, int, EquipmentSlotType> OnEquipItem;               // (charId, equipInstanceId, slot)
    public event Action<int, EquipmentSlotType>      OnUnequipItem;             // (charId, slot)
    public event Action<int>                         OnEnhanceEquipment;        // (equipInstanceId)
    public event Action<int>                         OnEnhancePreviewRequested; // (equipInstanceId)
    public event Action<int, int>                    OnTranscendWithShards;     // (characterId, shardsToUse)

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
            inventoryUI.OnItemClicked      += HandleItemClicked;
            inventoryUI.OnItemRightClicked += HandleItemRightClicked;
            inventoryUI.OnItemHoverEnter   += HandleItemHoverEnter;
            inventoryUI.OnItemHoverExit    += HandleItemHoverExit;
            inventoryUI.OnItemDragBegin    += HandleItemDragBegin;
        }

        foreach (var slot in _equipmentSlots)
        {
            slot.OnItemDropped += HandleEquip;
            slot.OnItemClicked += HandleUnequip;
        }

        if (enhancePanel   != null) enhancePanel.OnEnhanceRequested     += HandleEnhancePanelRequest;
        if (transcendPanel != null) transcendPanel.OnTranscendRequested  += HandleTranscendPanelRequest;

        sidebarManager.OnTabChanged += SwitchSubPanel;
        sidebarManager.Init();
    }

    private void OnDisable()
    {
        foreach (var inventoryUI in _itemInventoryUIs)
        {
            inventoryUI.OnItemClicked      -= HandleItemClicked;
            inventoryUI.OnItemRightClicked -= HandleItemRightClicked;
            inventoryUI.OnItemHoverEnter   -= HandleItemHoverEnter;
            inventoryUI.OnItemHoverExit    -= HandleItemHoverExit;
            inventoryUI.OnItemDragBegin    -= HandleItemDragBegin;
        }

        foreach (var slot in _equipmentSlots)
        {
            slot.OnItemDropped -= HandleEquip;
            slot.OnItemClicked -= HandleUnequip;
        }

        if (enhancePanel   != null) enhancePanel.OnEnhanceRequested    -= HandleEnhancePanelRequest;
        if (transcendPanel != null) transcendPanel.OnTranscendRequested -= HandleTranscendPanelRequest;

        sidebarManager.OnTabChanged -= SwitchSubPanel;
    }

    private void HandleEnhancePanelRequest(int id)           => OnEnhanceEquipment?.Invoke(id);
    private void HandleTranscendPanelRequest(int charId, int shardsToUse) => OnTranscendWithShards?.Invoke(charId, shardsToUse);

    // ── 열기 / 닫기 ───────────────────────────────────────────────────

    public void Open()  => panel.SetActive(true);
    public void Close()
    {
        panel.SetActive(false);
        modelManager.ClearAllModels();
        itemInfoPanel.Hide();
        actionPopup?.Hide();
        enhancePanel?.Hide();
    }

    // ── 초기화 ────────────────────────────────────────────────────────

    public void InitInventory(
        IEnumerable<PlayerCharacterData> charDatas,
        IEnumerable<PlayerItemData>      itemDatas,
        IEnumerable<PlayerEquipmentData> equipmentDatas)
    {
        _cachedCharacterDatas = charDatas     != null ? charDatas.ToList()      : new();
        _cachedItemDatas      = itemDatas      != null ? itemDatas.ToList()      : new();
        _cachedEquipmentDatas = equipmentDatas != null ? equipmentDatas.ToList() : new();

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
        enhancePanel?.Hide();
    }

    // ── 슬롯 이벤트 핸들러 ───────────────────────────────────────────

    private void HandleItemHoverEnter(int id, Vector2 pos, ItemType sourceType)
    {
        if (sourceType == ItemType.Equipment)
        {
            var equipData = _cachedEquipmentDatas.FirstOrDefault(e => e.equip_instance_id == id);
            if (equipData == null) return;
            var staticData = GameDataManager.Instance.GetItem(equipData.itemId);
            if (staticData != null) { itemInfoPanel.SetEquipmentData(staticData, equipData.enhance); itemInfoPanel.ShowAt(pos); }
            return;
        }

        if (sourceType == ItemType.Transcendence)
        {
            var charData   = _cachedCharacterDatas.FirstOrDefault(c => c.characterId == id);
            if (charData == null) return;
            var staticData = GameDataManager.Instance.GetCharacter(id);
            if (staticData != null) { itemInfoPanel.SetShardData(staticData, charData.shardAmount); itemInfoPanel.ShowAt(pos); }
            return;
        }

        var itemData = _cachedItemDatas.FirstOrDefault(i => i.itemId == id);
        if (itemData != null)
        {
            var staticData = GameDataManager.Instance.GetItem(id);
            itemInfoPanel.SetData(staticData, itemData.amount);
            itemInfoPanel.ShowAt(pos);
        }
    }

    private void HandleItemHoverExit() => itemInfoPanel.Hide();

    private void HandleItemClicked(int id, Vector2 pos, ItemType sourceType)
    {
        if (sourceType == ItemType.Equipment)
        {
            var equipData = _cachedEquipmentDatas.FirstOrDefault(e => e.equip_instance_id == id);
            if (equipData != null)
            {
                var staticData = GameDataManager.Instance.GetItem(equipData.itemId);
                if (staticData != null)
                {
                    actionPopup.ShowEquipment(pos, actionType =>
                    {
                        if (actionType == ItemActionType.Enhance)
                        {
                            enhancePanel?.Open(id, staticData.displayName, staticData.icon);
                            OnEnhancePreviewRequested?.Invoke(id);
                        }
                        else if (actionType == ItemActionType.Equip)
                        {
                            OnEquipItem?.Invoke(_selectedCharacterId, id, staticData.slotType);
                        }
                    });
                }
            }
            foreach (var ui in _itemInventoryUIs) ui.RefreshSelection(id);
            return;
        }

        if (sourceType == ItemType.Transcendence)
        {
            foreach (var ui in _itemInventoryUIs) ui.RefreshSelection(id);
            return;
        }

        // Consumable
        var data = GameDataManager.Instance.GetItem(id);
        if (data == null) return;
        actionPopup.Show(data, pos, actionType =>
        {
            if (actionType == ItemActionType.Use)     OnUseItem?.Invoke(_selectedCharacterId, id);
            if (actionType == ItemActionType.Discard) OnDiscardItem?.Invoke(id);
        });
        foreach (var ui in _itemInventoryUIs) ui.RefreshSelection(id);
    }

    private void HandleItemRightClicked(int id, Vector2 pos, ItemType sourceType)
    {
        if (sourceType != ItemType.Equipment) return;
        var equipData  = _cachedEquipmentDatas.FirstOrDefault(e => e.equip_instance_id == id);
        if (equipData == null) return;
        var staticData = GameDataManager.Instance.GetItem(equipData.itemId);
        if (staticData == null) return;
        OnEquipItem?.Invoke(_selectedCharacterId, id, staticData.slotType);
    }

    private void HandleItemDragBegin(int id, ItemType sourceType)
    {
        itemInfoPanel.Hide();
        actionPopup?.Hide();

        if (sourceType == ItemType.Equipment)
        {
            var equipData = _cachedEquipmentDatas.FirstOrDefault(e => e.equip_instance_id == id);
            if (equipData != null)
            {
                var staticData = GameDataManager.Instance.GetItem(equipData.itemId);
                if (staticData != null)
                    DragController.Instance?.BeginDrag(staticData, equipData.equip_instance_id);
            }
            return;
        }

        var data = GameDataManager.Instance.GetItem(id);
        if (data != null) DragController.Instance?.BeginDrag(data);
    }

    // ── 강화 패널 응답 ────────────────────────────────────────────────

    public void ShowEnhancePreview(int currentEnhance, float successRate, int goldCost)
        => enhancePanel?.SetPreviewData(currentEnhance, successRate, goldCost);

    public void ShowEnhanceMaxEnhance()
    {
        enhancePanel?.SetMaxEnhance();
        messagePanel?.Show("최대 강화 상태입니다.", UIMessageType.Info);
    }

    public void ShowEnhanceResult(bool success, int enhance)
    {
        enhancePanel?.ShowResult(success, enhance);
        messagePanel?.Show(
            success ? $"강화 성공!  (+{enhance})" : "강화 실패...",
            success ? UIMessageType.Success : UIMessageType.Fail);
    }

    public void ShowEnhanceError(string message)
    {
        enhancePanel?.ShowError(message);
        messagePanel?.Show(message, UIMessageType.Error);
    }

    // ── 초월 패널 응답 ────────────────────────────────────────────────

    public void ShowTranscendResult(bool transcendSuccess, int transcendStage)
    {
        transcendPanel?.ShowResult(transcendSuccess, transcendStage);
        messagePanel?.Show(
            transcendSuccess ? $"초월 성공! ({transcendStage}단계 달성)" : "초월 실패... 조각은 소모됩니다.",
            transcendSuccess ? UIMessageType.Success : UIMessageType.Fail);
    }

    public void ShowTranscendError(string message)
    {
        transcendPanel?.ShowError(message);
        messagePanel?.Show(message, UIMessageType.Error);
    }

    // ── 장착 슬롯 ─────────────────────────────────────────────────────

    private void HandleEquip(int equipInstanceId, EquipmentSlotType slotType)
        => OnEquipItem?.Invoke(_selectedCharacterId, equipInstanceId, slotType);

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
                if (staticData == null) { slot.Clear(); continue; }
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
        transcendPanel?.Init(id, selectedModel.ServerData.enhance, selectedModel.ServerData.shardAmount);
        RefreshActiveItemUI();
    }
}
