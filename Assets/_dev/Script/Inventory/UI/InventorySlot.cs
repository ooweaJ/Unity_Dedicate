using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Image selectFrameImage;
    [SerializeField] private Button slotButton;

    [Header("Selection Colors")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.75f, 0f);
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0f); // 투명

    private int _id;
    public int Id => _id;

    private Action<int> _onClicked;
    private Action<int, Vector2> _onHoverEnter;
    private Action _onHoverExit;

    private void Awake()
    {
        if (slotButton == null) slotButton = GetComponent<Button>();
        slotButton.onClick.AddListener(OnSlotClick);
    }

    public void Setup(int id, Sprite icon, int amount, Action<int> onClicked, Action<int, Vector2> onHoverEnter = null, Action onHoverExit = null)
    {
        _id = id;
        _onClicked = onClicked;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;

        if (iconImage != null) iconImage.sprite = icon;
        if (amountText != null) 
        {
            amountText.text = amount > 1 ? amount.ToString() : "";
        }

        SetSelect(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onHoverEnter?.Invoke(_id, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onHoverExit?.Invoke();
    }

    public void SetSelect(bool isSelected)
    {
        if (selectFrameImage != null)
        {
            selectFrameImage.color = isSelected ? selectedColor : unselectedColor;
        }
    }

    private void OnSlotClick()
    {
        _onClicked?.Invoke(_id);
    }
}