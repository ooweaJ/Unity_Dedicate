using UnityEngine;
using UnityEngine.UI;
using System;

public class BannerListItem : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private GameObject dimOverlay;
    [SerializeField] private Button button;

    // 선택 시 버튼 테두리 색
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color deselectedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private BannerData _data;
    private Action<BannerData> _onSelect;

    public BannerData GetData() => _data;

    public void Setup(BannerData data, Action<BannerData> onSelect)
    {
        _data = data;
        _onSelect = onSelect;

        thumbnailImage.sprite = data.thumbnailSprite;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _onSelect?.Invoke(_data));

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        // 버튼 이미지 색으로 선택 표시
        button.image.color = isSelected ? selectedColor : deselectedColor;

        // 비선택 시 어둡게
        dimOverlay.SetActive(!isSelected);
    }
}