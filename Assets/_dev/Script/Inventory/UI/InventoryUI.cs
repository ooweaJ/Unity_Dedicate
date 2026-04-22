using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class InventoryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ItemType targetType; // 인스펙터에서 설정 (Equipment, Consumable, Transcendence 등)
    
    [Header("UI References")]
    [SerializeField] private InventorySlot slotPrefab;
    [SerializeField] private Transform contentParent; // ScrollView의 Content 오브젝트

    private List<InventorySlot> _slots = new();
    private int _selectedId = -1;

    // ✅ 데이터 제공자들
    private Func<IEnumerable<PlayerItemData>> _itemDataProvider;
    private Func<IEnumerable<PlayerCharacterData>> _charDataProvider; // 초월 재료용
    private Func<int> _selectedCharacterIdProvider;                   // 현재 선택된 캐릭터 확인용

    // Manager로 전달할 이벤트들
    public event Action<int, Vector2> OnItemClicked;
    public event Action<int, Vector2> OnItemHoverEnter;
    public event Action OnItemHoverExit;
    public event Action<int> OnItemDragBegin;

    public void Setup(
        Func<IEnumerable<PlayerItemData>> itemDataProvider, 
        Func<IEnumerable<PlayerCharacterData>> charDataProvider,
        Func<int> selectedCharacterIdProvider)
    {
        _itemDataProvider = itemDataProvider;
        _charDataProvider = charDataProvider;
        _selectedCharacterIdProvider = selectedCharacterIdProvider;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        // 1. 기존 슬롯 제거 및 이벤트 해제
        foreach (var slot in _slots)
        {
            if (slot == null) continue;
            slot.OnClicked -= HandleSlotClicked;
            slot.OnHoverEnter -= HandleSlotHoverEnter;
            slot.OnHoverExit -= HandleSlotHoverExit;
            slot.OnDragBegin -= HandleSlotDragBegin;
            Destroy(slot.gameObject);
        }
        _slots.Clear();

        // 2. 타입에 따른 데이터 처리
        if (targetType == ItemType.Transcendence)
        {
            RefreshTranscendenceSlots();
        }
        else
        {
            RefreshItemSlots();
        }
    }

    private void RefreshTranscendenceSlots()
    {
        if (_charDataProvider == null || _selectedCharacterIdProvider == null) return;

        int selectedCharId = _selectedCharacterIdProvider.Invoke();
        var allChars = _charDataProvider.Invoke();
        
        var targetChar = allChars.FirstOrDefault(c => c.characterId == selectedCharId);
        if (targetChar == null || targetChar.shardAmount <= 0) return;

        var staticData = GameDataManager.Instance.GetCharacter(targetChar.characterId);
        if (staticData == null) return;

        CreateSlot(targetChar.characterId, staticData.shardIcon, targetChar.shardAmount, ItemType.TranscendShard);
    }

    private void RefreshItemSlots()
    {
        if (_itemDataProvider == null) return;

        var allItems = _itemDataProvider.Invoke();
        if (allItems == null) return;

        foreach (var item in allItems)
        {
            var itemData = GameDataManager.Instance.GetItem(item.itemId);
            if (itemData == null || itemData.itemType != targetType) continue;

            CreateSlot(item.itemId, itemData.icon, item.amount, itemData.itemType);
        }
    }

    private void CreateSlot(int id, Sprite icon, int amount, ItemType type)
    {
        var slot = Instantiate(slotPrefab, contentParent);
        var staticData = GameDataManager.Instance.GetItem(id); // 기본 아이템 데이터 가져오기
        
        // 만약 캐릭터 조각이라면 가상의 ItemRawData 생성 (또는 Table에 조각도 포함되어 있어야 함)
        if (staticData == null && type == ItemType.TranscendShard)
        {
            staticData = new ItemRawData { id = id, icon = icon, itemType = type, displayName = "조각" };
        }

        slot.SetItem(id, staticData, amount);
        
        // 이벤트 연결
        slot.OnClicked += HandleSlotClicked;
        slot.OnHoverEnter += HandleSlotHoverEnter;
        slot.OnHoverExit += HandleSlotHoverExit;
        slot.OnDragBegin += HandleSlotDragBegin;
        
        _slots.Add(slot);
    }

    private void HandleSlotClicked(int id, Vector2 pos) => OnItemClicked?.Invoke(id, pos);
    private void HandleSlotHoverEnter(int id, Vector2 pos) => OnItemHoverEnter?.Invoke(id, pos);
    private void HandleSlotHoverExit() => OnItemHoverExit?.Invoke();
    private void HandleSlotDragBegin(int id) => OnItemDragBegin?.Invoke(id);

    public void RefreshSelection(int selectedId)
    {
        _selectedId = selectedId;
        foreach (var slot in _slots)
        {
            slot.SetSelect(slot.ItemId == selectedId);
        }
    }
}
