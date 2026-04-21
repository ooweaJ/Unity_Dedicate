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

    public event Action<int> OnItemSelected;
    public event Action<int, Vector2> OnItemHoverEnter;
    public event Action OnItemHoverExit;

    /// <summary>
    /// 초기 설정: 데이터를 어디서 가져올지 정의합니다.
    /// </summary>
    public void Setup(
        Func<IEnumerable<PlayerItemData>> itemDataProvider, 
        Func<IEnumerable<PlayerCharacterData>> charDataProvider,
        Func<int> selectedCharacterIdProvider,
        Action<int> onItemSelected = null,
        Action<int, Vector2> onHoverEnter = null,
        Action onHoverExit = null)
    {
        _itemDataProvider = itemDataProvider;
        _charDataProvider = charDataProvider;
        _selectedCharacterIdProvider = selectedCharacterIdProvider;
        OnItemSelected = onItemSelected;
        OnItemHoverEnter = onHoverEnter;
        OnItemHoverExit = onHoverExit;
    }

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>
    /// 실제 슬롯을 생성하고 데이터를 화면에 그립니다.
    /// </summary>
    public void Refresh()
    {
        // 1. 기존 슬롯 제거
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
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

        // 3. 초기 선택 처리
        if (_slots.Count > 0)
        {
            OnSlotClickedInternal(_slots[0].Id);
        }
    }

    private void RefreshTranscendenceSlots()
    {
        if (_charDataProvider == null || _selectedCharacterIdProvider == null) return;

        int selectedCharId = _selectedCharacterIdProvider.Invoke();
        var allChars = _charDataProvider.Invoke();
        
        // ✅ 현재 선택된 캐릭터의 데이터만 찾아서 조각 정보를 표시
        var targetChar = allChars.FirstOrDefault(c => c.characterId == selectedCharId);
        if (targetChar == null) return;

        if (targetChar.shardAmount <= 0) return;

        var staticData = GameDataManager.Instance.GetCharacter(targetChar.characterId);
        if (staticData == null) return;

        // 조각 슬롯 생성 (ID는 캐릭터 ID를 그대로 사용하거나 별도 규칙 적용 가능)
        var slot = Instantiate(slotPrefab, contentParent);
        slot.Setup(
            targetChar.characterId, 
            staticData.shardIcon, 
            targetChar.shardAmount, 
            OnSlotClickedInternal,
            OnItemHoverEnter,
            OnItemHoverExit
        );
        _slots.Add(slot);
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

            var slot = Instantiate(slotPrefab, contentParent);
            slot.Setup(
                item.itemId, 
                itemData.icon, 
                item.amount, 
                OnSlotClickedInternal,
                OnItemHoverEnter,
                OnItemHoverExit
            );
            _slots.Add(slot);
        }
    }

    private void OnSlotClickedInternal(int id)
    {
        _selectedId = id;
        RefreshSelection(id);
        OnItemSelected?.Invoke(id);
    }

    public void RefreshSelection(int selectedId)
    {
        foreach (var slot in _slots)
        {
            slot.SetSelect(slot.Id == selectedId);
        }
    }
}