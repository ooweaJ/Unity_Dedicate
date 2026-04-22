using UnityEngine;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// 장비 전용 슬롯: 특정 장비 타입(무기, 갑옷 등)만 허용하고 드롭을 통해 장착
/// </summary>
public class EquipmentSlot : BaseSlot, IDropHandler, IPointerClickHandler
{
    [SerializeField] public EquipmentSlotType acceptedSlotType;

    // Manager가 구독할 이벤트들
    public event Action<int, EquipmentSlotType> OnItemDropped;  // 장착 요청
    public event Action<int>                     OnItemClicked;  // 해제 요청

    public void OnDrop(PointerEventData e)
    {
        if (DragController.Instance == null) return;
        
        var dragging = DragController.Instance.DraggingData;
        if (dragging == null) return;

        // 테이블 기반 구조이므로 EquipmentRawData 캐스팅 대신 StaticData의 필드 확인
        if (dragging.itemType != ItemType.Equipment || dragging.slotType != acceptedSlotType)
        {
            Debug.Log($"[EquipmentSlot] Invalid type: {dragging.slotType} != {acceptedSlotType}");
            return;
        }

        OnItemDropped?.Invoke(dragging.id, acceptedSlotType);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!IsEmpty) OnItemClicked?.Invoke(ItemId); // 클릭 시 해제 요청
    }
}
