using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class InventoryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ItemType targetType; // 인스펙터에서 설정 (Equipment, Consumable 등)
    
    [Header("UI References")]
    [SerializeField] private InventorySlot slotPrefab;
    [SerializeField] private Transform contentParent; // ScrollView의 Content 오브젝트

    private List<InventorySlot> _slots = new();
    private int _selectedId = -1;

    // 아이템 선택 시 외부(상세 정보 창 등)에 알릴 이벤트
    public event Action<int> OnItemSelected;

    /// <summary>
    /// 데이터를 주입받아 슬롯을 생성하고 초기화합니다. (CharacterListManager 패턴)
    /// </summary>
    /// 

    public void Init(IEnumerable<PlayerItemData> allItems, Action<int> onItemSelected = null)
    {
        OnItemSelected = onItemSelected;

        // 1. 기존 슬롯 제거
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        _slots.Clear();

        // 2. 내 타입에 맞는 데이터만 필터링
        var filteredItems = allItems.Where(item => 
        {
            var staticData = GameDataManager.Instance.GetItem(item.itemId);
            return staticData != null && staticData.itemType == targetType;
        }).ToList();

        // 3. 필터링된 데이터로 슬롯 생성
        foreach (var item in filteredItems)
        {
            var staticData = GameDataManager.Instance.GetItem(item.itemId);
            var slot = Instantiate(slotPrefab, contentParent);
            
            slot.Setup(
                item.itemId, 
                staticData.icon, 
                item.amoutn, // 데이터의 오타(amoutn) 유지
                OnSlotClickedInternal
            );

            _slots.Add(slot);
        }

        // 4. 초기 선택 처리 (첫 번째 아이템 자동 선택)
        if (_slots.Count > 0)
        {
            OnSlotClickedInternal(_slots[0].Id);
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