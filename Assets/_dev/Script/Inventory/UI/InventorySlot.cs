using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 슬롯: 아이템 표시, 클릭(소비/재료), 드래그(장비) 담당
/// </summary>
public class InventorySlot : BaseSlot, 
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Selection UI")]
    [SerializeField] private Image selectFrameImage;
    [SerializeField] private Color selectedColor = new Color(1f, 0.75f, 0f);
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0f);

    // Manager가 구독할 이벤트들
    public event Action<int, Vector2> OnHoverEnter;
    public event Action              OnHoverExit;
    public event Action<int, Vector2> OnClicked;    // 클릭 (소비템/재료)
    public event Action<int>          OnDragBegin;  // 드래그 시작 (장비)

    public override void SetItem(int itemId, ItemRawData data, int amount)
    {
        base.SetItem(itemId, data, amount);
        SetSelect(false);
    }

    // --- 이벤트 핸들러 ---

    public void OnPointerEnter(PointerEventData e)
    {
        if (!IsEmpty) OnHoverEnter?.Invoke(ItemId, e.position);
    }

    public void OnPointerExit(PointerEventData e) => OnHoverExit?.Invoke();

    public void OnPointerClick(PointerEventData e)
    {
        if (IsEmpty) return;
        
        // 장비는 클릭 대신 드래그로 처리할 경우 (advice 기준)
        if (StaticData.itemType == ItemType.Equipment) return;
        
        OnClicked?.Invoke(ItemId, e.position);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (IsEmpty || StaticData.itemType != ItemType.Equipment) return;
        
        OnDragBegin?.Invoke(ItemId);
    }

    public void OnDrag(PointerEventData e)
    {
        if (DragController.Instance != null)
            DragController.Instance.OnDrag(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (DragController.Instance != null)
            DragController.Instance.OnEndDrag(e);
    }

    // --- 시각적 효과 ---

    public void SetSelect(bool isSelected)
    {
        if (selectFrameImage != null)
        {
            selectFrameImage.color = isSelected ? selectedColor : unselectedColor;
        }
    }
}
