using UnityEngine;
using UnityEngine.UI;
using System;

// 버튼 컴포넌트가 필수적으로 있어야 함을 명시 (실수 방지)
[RequireComponent(typeof(Button))]
public class CharacterSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image portraitIcon;
    [SerializeField] private Image selectFrameImage;
    [SerializeField] private Button slotButton; // 버튼 컴포넌트 참조

    [Header("Selection Colors")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.75f, 0f);
    [SerializeField] private Color unselectedColor = Color.white;

    private int _id;
    public int Id => _id;

    private CharacterUIModel _model;
    private Action<int> _onClicked;

    private void Awake()
    {
        // 만약 인스펙터에서 버튼을 할당 안 했다면 자동으로 찾아옵니다.
        if (slotButton == null)
            slotButton = GetComponent<Button>();

        // 버튼 클릭 이벤트 리스너 등록
        slotButton.onClick.AddListener(OnSlotClick);
    }

    public void Setup(CharacterUIModel model, Action<int> onClicked)
    {
        _model = model;
        _onClicked = onClicked;
        _id = model.CharacterId;

        if (portraitIcon != null)
            portraitIcon.sprite = model.Icon;

        SetSelect(false);
    }

    public void SetSelect(bool isSelected)
    {
        if (selectFrameImage != null)
        {
            selectFrameImage.color = isSelected ? selectedColor : unselectedColor;
        }

        // 선택된 슬롯은 버튼을 잠시 비활성화하거나, 
        // 시각적으로 더 강조하기 위해 버튼의 interactable을 건드릴 수도 있습니다.
    }

    private void OnSlotClick()
    {
        if (_model != null)
        {
            // 부모(Manager)에게 내 ID를 던져줌
            _onClicked?.Invoke(_model.CharacterId);
        }
    }
}