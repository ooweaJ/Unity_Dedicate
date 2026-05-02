using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class InventoryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ItemType targetType;

    [Header("UI References")]
    [SerializeField] private InventorySlot slotPrefab;
    [SerializeField] private Transform     contentParent;

    private List<InventorySlot> _slots = new();
    private int _selectedId = -1;

    // 데이터 제공자
    private Func<IEnumerable<PlayerItemData>>      _itemDataProvider;
    private Func<IEnumerable<PlayerEquipmentData>> _equipmentDataProvider;
    private Func<IEnumerable<PlayerCharacterData>> _charDataProvider;
    private Func<int>                              _selectedCharacterIdProvider;

    public event Action<int, Vector2> OnItemClicked;
    public event Action<int, Vector2> OnItemHoverEnter;
    public event Action               OnItemHoverExit;
    public event Action<int>          OnItemDragBegin;

    public void Setup(
        Func<IEnumerable<PlayerItemData>>      itemDataProvider,
        Func<IEnumerable<PlayerEquipmentData>> equipmentDataProvider,
        Func<IEnumerable<PlayerCharacterData>> charDataProvider,
        Func<int>                              selectedCharacterIdProvider)
    {
        _itemDataProvider            = itemDataProvider;
        _equipmentDataProvider       = equipmentDataProvider;
        _charDataProvider            = charDataProvider;
        _selectedCharacterIdProvider = selectedCharacterIdProvider;
    }

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        foreach (var slot in _slots)
        {
            if (slot == null) continue;
            slot.OnClicked    -= HandleSlotClicked;
            slot.OnHoverEnter -= HandleSlotHoverEnter;
            slot.OnHoverExit  -= HandleSlotHoverExit;
            slot.OnDragBegin  -= HandleSlotDragBegin;
            Destroy(slot.gameObject);
        }
        _slots.Clear();

        if (targetType == ItemType.Transcendence)
            RefreshTranscendenceSlots();
        else if (targetType == ItemType.Equipment)
            RefreshEquipmentInstanceSlots();
        else
            RefreshItemSlots();
    }

    // ── 초월 조각 탭 ──────────────────────────────────────────────────

    private void RefreshTranscendenceSlots()
    {
        if (_charDataProvider == null || _selectedCharacterIdProvider == null) return;

        int selectedCharId = _selectedCharacterIdProvider.Invoke();
        var targetChar = _charDataProvider.Invoke()
            .FirstOrDefault(c => c.characterId == selectedCharId);

        if (targetChar == null || targetChar.shardAmount <= 0) return;

        var staticData = GameDataManager.Instance.GetCharacter(targetChar.characterId);
        if (staticData == null) return;

        CreateSlot(targetChar.characterId, staticData.shardIcon, targetChar.shardAmount,
            new ItemRawData { id = targetChar.characterId, icon = staticData.shardIcon,
                              itemType = ItemType.Transcendence, displayName = "조각" });
    }

    // ── 장비 인스턴스 탭 ──────────────────────────────────────────────
    // 슬롯 ID = equip_instance_id (강화 수치를 표시하기 위해)

    private void RefreshEquipmentInstanceSlots()
    {
        if (_equipmentDataProvider == null) return;

        foreach (var equip in _equipmentDataProvider.Invoke())
        {
            var staticData = GameDataManager.Instance.GetItem(equip.itemId);
            if (staticData == null) continue;

            // amount 자리에 enhance 표시 (슬롯 UI에서 수량 badge에 강화 단계 표시)
            CreateSlot(equip.equip_instance_id, staticData.icon, equip.enhance, staticData);
        }
    }

    // ── 소모품/재료 탭 ────────────────────────────────────────────────

    private void RefreshItemSlots()
    {
        if (_itemDataProvider == null) return;

        foreach (var item in _itemDataProvider.Invoke())
        {
            var itemData = GameDataManager.Instance.GetItem(item.itemId);
            if (itemData == null || itemData.itemType != targetType) continue;

            CreateSlot(item.itemId, itemData.icon, item.amount, itemData);
        }
    }

    // ── 슬롯 생성 ─────────────────────────────────────────────────────

    private void CreateSlot(int id, Sprite icon, int amount, ItemRawData staticData)
    {
        var slot = Instantiate(slotPrefab, contentParent);
        slot.SetItem(id, staticData, amount);

        slot.OnClicked    += HandleSlotClicked;
        slot.OnHoverEnter += HandleSlotHoverEnter;
        slot.OnHoverExit  += HandleSlotHoverExit;
        slot.OnDragBegin  += HandleSlotDragBegin;

        _slots.Add(slot);
    }

    // icon 전달 버전 (Transcendence 슬롯 등 staticData가 없을 때)
    private void CreateSlot(int id, Sprite icon, int amount, ItemType type, ItemRawData explicitData = null)
    {
        var staticData = explicitData ?? GameDataManager.Instance.GetItem(id);
        if (staticData == null)
            staticData = new ItemRawData { id = id, icon = icon, itemType = type, displayName = "" };
        CreateSlot(id, icon, amount, staticData);
    }

    private void HandleSlotClicked(int id, Vector2 pos)    => OnItemClicked?.Invoke(id, pos);
    private void HandleSlotHoverEnter(int id, Vector2 pos) => OnItemHoverEnter?.Invoke(id, pos);
    private void HandleSlotHoverExit()                      => OnItemHoverExit?.Invoke();
    private void HandleSlotDragBegin(int id)               => OnItemDragBegin?.Invoke(id);

    public void RefreshSelection(int selectedId)
    {
        _selectedId = selectedId;
        foreach (var slot in _slots)
            slot.SetSelect(slot.ItemId == selectedId);
    }
}
