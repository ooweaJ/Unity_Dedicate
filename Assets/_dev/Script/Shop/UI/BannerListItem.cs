using UnityEngine;
using UnityEngine.UI;
using System;

public class BannerListItem : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private GameObject dimOverlay;
    [SerializeField] private Button button;

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
        dimOverlay.SetActive(!isSelected);
    }
}