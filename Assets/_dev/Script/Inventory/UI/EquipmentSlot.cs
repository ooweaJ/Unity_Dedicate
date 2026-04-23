using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// 장비 전용 슬롯: 특정 장비 타입(무기, 갑옷 등)만 허용하고 드롭을 통해 장착
/// </summary>
public class EquipmentSlot : BaseSlot, IDropHandler, IPointerClickHandler
{
    [SerializeField] public EquipmentSlotType acceptedSlotType;
    [SerializeField] private Image baseImage; // 빈 슬롯일 때 표시할 기본 이미지

    public event Action<int, EquipmentSlotType> OnItemDropped;
    public event Action<int>                     OnItemClicked;

    public override void SetItem(int itemId, ItemRawData data, int amount)
    {
        base.SetItem(itemId, data, amount);
        if (baseImage != null) baseImage.gameObject.SetActive(false);
    }

    public override void Clear()
    {
        base.Clear();
        if (baseImage != null) baseImage.gameObject.SetActive(true);
    }

    public void OnDrop(PointerEventData e)
    {
        if (DragController.Instance == null) return;

        var dragging = DragController.Instance.DraggingData;
        if (dragging == null) return;

        if (dragging.itemType != ItemType.Equipment || dragging.slotType != acceptedSlotType)
        {
            Debug.Log($"[EquipmentSlot] Invalid type: {dragging.slotType} != {acceptedSlotType}");
            return;
        }
       
        OnItemDropped?.Invoke(dragging.id, acceptedSlotType);

        if (DragController.Instance != null)
        {
            DragController.Instance.OnEndDrag(e);
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!IsEmpty) OnItemClicked?.Invoke(ItemId);
    }
}
