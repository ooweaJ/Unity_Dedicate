using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[RequireComponent(typeof(Button))]
public class InventorySlot : MonoBehaviour
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

    private void Awake()
    {
        if (slotButton == null) slotButton = GetComponent<Button>();
        slotButton.onClick.AddListener(OnSlotClick);
    }

    public void Setup(int id, Sprite icon, int amount, Action<int> onClicked)
    {
        _id = id;
        _onClicked = onClicked;

        if (iconImage != null) iconImage.sprite = icon;
        if (amountText != null) 
        {
            // 수량이 1개보다 많을 때만 텍스트 표시 (선택 사항)
            amountText.text = amount > 1 ? amount.ToString() : "";
        }

        SetSelect(false);
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