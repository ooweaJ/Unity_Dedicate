using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtDescription;
    [SerializeField] private TextMeshProUGUI txtAmount;
    [SerializeField] private Image imgIcon;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(15f, 15f);

    private RectTransform _rect;
    private Canvas _rootCanvas;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.rootCanvas;

        // ✅ 핵심: 패널이 마우스 이벤트 막지 않도록 (깜빡임 원인 제거)

        Hide();
    }

    public void ShowAt(Vector2 screenPosition)
    {
        gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases(); // 레이아웃 즉시 갱신 → 크기 바로 읽기 가능
        ApplyPosition(screenPosition);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ApplyPosition(Vector2 screenPosition)
    {
        float scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
        Vector2 panelSize = _rect.rect.size * scaleFactor;
        Vector2 pos = screenPosition + offset;

        // 화면 경계 체크
        if (pos.x + panelSize.x > Screen.width)
            pos.x = screenPosition.x - panelSize.x - offset.x;

        if (pos.y + panelSize.y > Screen.height)
            pos.y = screenPosition.y - panelSize.y - offset.y;

        pos.x = Mathf.Clamp(pos.x, 0f, Screen.width - panelSize.x);
        pos.y = Mathf.Clamp(pos.y, 0f, Screen.height - panelSize.y);

        _rect.position = pos;
    }

    public void SetData(ItemRawData staticData, int amount)
    {
        if (staticData == null) return;
        txtName.text = staticData.displayName;
        txtDescription.text = staticData.description;
        txtAmount.text = $"보유 수량: {amount}";
        if (imgIcon != null) imgIcon.sprite = staticData.icon;
    }

    public void SetShardData(CharacterRawData charData, int amount)
    {
        if (charData == null) return;
        txtName.text = $"{charData.displayName} 조각";
        txtDescription.text = $"{charData.displayName}의 초월에 필요한 재료입니다.";
        txtAmount.text = $"보유 수량: {amount}";
        if (imgIcon != null) imgIcon.sprite = charData.shardIcon;
    }
}