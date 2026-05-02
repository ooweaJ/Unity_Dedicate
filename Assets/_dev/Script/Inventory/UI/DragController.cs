using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour
{
    public static DragController Instance { get; private set; }

    [SerializeField] private Image dragIcon;

    public ItemRawData DraggingData       { get; private set; }
    public int         DraggingInstanceId { get; private set; } = -1; // 장비 인스턴스 ID

    private void Awake()
    {
        Instance = this;
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);
    }

    // 일반 아이템 드래그
    public void BeginDrag(ItemRawData data) => BeginDrag(data, -1);

    // 장비 인스턴스 드래그 (equipInstanceId 포함)
    public void BeginDrag(ItemRawData data, int equipInstanceId)
    {
        DraggingData       = data;
        DraggingInstanceId = equipInstanceId;

        if (dragIcon != null)
        {
            dragIcon.sprite        = data.icon;
            dragIcon.raycastTarget = false;
            dragIcon.gameObject.SetActive(true);
        }
    }

    public void OnDrag(Vector2 screenPos)
    {
        if (DraggingData == null || dragIcon == null) return;
        dragIcon.transform.position = screenPos;
    }

    public void OnEndDrag(PointerEventData e) => EndDrag();

    public void EndDrag()
    {
        DraggingData       = null;
        DraggingInstanceId = -1;
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);
    }
}
