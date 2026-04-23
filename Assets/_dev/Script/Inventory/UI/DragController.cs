using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour
{
    public static DragController Instance { get; private set; }

    [SerializeField] private Image dragIcon;   // Canvas 최상단 이미지
    public ItemRawData DraggingData { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);
    }

    public void BeginDrag(ItemRawData data)
    {
        DraggingData = data;
        if (dragIcon != null)
        {
            dragIcon.sprite = data.icon;
            dragIcon.raycastTarget = false; // EquipmentSlot이 OnDrop을 받을 수 있도록 반드시 false
            dragIcon.gameObject.SetActive(true);
        }
    }

    public void OnDrag(Vector2 screenPos)
    {
        if (DraggingData == null || dragIcon == null) return;
        dragIcon.transform.position = screenPos;
    }

    public void OnEndDrag(PointerEventData e)
    {
        EndDrag();
    }

    // InventorySlot.OnEndDrag 외부에서도 강제 정리할 수 있도록 public
    public void EndDrag()
    {
        DraggingData = null;
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);
    }
}
