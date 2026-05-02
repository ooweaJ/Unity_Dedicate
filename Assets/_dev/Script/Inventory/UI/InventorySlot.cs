using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 슬롯: 아이템/장비 표시, 클릭, 드래그 담당
/// 장비 인스턴스는 equip_instance_id가 ItemId에 저장됩니다.
/// </summary>
public class InventorySlot : BaseSlot,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Selection UI")]
    [SerializeField] private Image selectFrameImage;
    [SerializeField] private Color selectedColor   = new Color(1f, 0.75f, 0f);
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0f);

    public event Action<int, Vector2> OnHoverEnter;
    public event Action               OnHoverExit;
    public event Action<int, Vector2> OnClicked;   // 클릭 (소비템/재료/장비 강화)
    public event Action<int>          OnDragBegin; // 드래그 시작 (장비 장착)

    public override void SetItem(int itemId, ItemRawData data, int amount)
    {
        base.SetItem(itemId, data, amount);
        SetSelect(false);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!IsEmpty) OnHoverEnter?.Invoke(ItemId, e.position);
    }

    public void OnPointerExit(PointerEventData e) => OnHoverExit?.Invoke();

    public void OnPointerClick(PointerEventData e)
    {
        if (IsEmpty) return;
        OnClicked?.Invoke(ItemId, e.position);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        // 장비만 드래그 가능
        if (IsEmpty || StaticData == null || StaticData.itemType != ItemType.Equipment) return;
        OnDragBegin?.Invoke(ItemId);
    }

    public void OnDrag(PointerEventData e)
    {
        DragController.Instance?.OnDrag(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        DragController.Instance?.EndDrag();
    }

    public void SetSelect(bool isSelected)
    {
        if (selectFrameImage != null)
            selectFrameImage.color = isSelected ? selectedColor : unselectedColor;
    }
}
