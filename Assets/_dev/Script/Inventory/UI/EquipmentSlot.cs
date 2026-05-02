using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// 장비 전용 슬롯: 특정 슬롯 타입만 허용, 드롭으로 장착
/// SetItem 시 itemId 대신 equip_instance_id를 넘겨 ItemId에 인스턴스 ID를 저장합니다.
/// </summary>
public class EquipmentSlot : BaseSlot, IDropHandler, IPointerClickHandler
{
    [SerializeField] public EquipmentSlotType acceptedSlotType;
    [SerializeField] private Image baseImage;

    // (equipInstanceId, slotType)
    public event Action<int, EquipmentSlotType> OnItemDropped;
    // (equipInstanceId)
    public event Action<int> OnItemClicked;

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
            Debug.Log($"[EquipmentSlot] 슬롯 타입 불일치: {dragging.slotType} != {acceptedSlotType}");
            return;
        }

        // DraggingInstanceId가 있으면 인스턴스 ID, 없으면 static item_id (하위 호환)
        int instanceId = DragController.Instance.DraggingInstanceId >= 0
            ? DragController.Instance.DraggingInstanceId
            : dragging.id;

        OnItemDropped?.Invoke(instanceId, acceptedSlotType);
        DragController.Instance.OnEndDrag(e);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!IsEmpty) OnItemClicked?.Invoke(ItemId); // ItemId = equip_instance_id
    }
}
