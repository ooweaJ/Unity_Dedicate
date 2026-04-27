using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 아이템 클릭 시 나타나는 액션 선택 팝업 (사용, 버리기, 초월 등)
/// </summary>
public class ItemActionPopup : MonoBehaviour
{
    [SerializeField] private Button btnPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private Vector2 offset = new Vector2(75, -100);
    private readonly List<Button> _buttons = new();

    public void Show(ItemRawData data, Vector2 position, Action<ItemActionType> onAction)
    {
        // 기존 버튼 제거
        foreach (var b in _buttons) 
        {
            if (b != null) Destroy(b.gameObject);
        }
        _buttons.Clear();

        if (data.actions == null || data.actions.Count == 0)
        {
            Hide();
            return;
        }

        // 데이터에 정의된 액션 버튼 생성
        foreach (var action in data.actions)
        {
            var btn = Instantiate(btnPrefab, container);
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = action.label;

            var capturedType = action.type;
            btn.onClick.AddListener(() =>
            {
                onAction?.Invoke(capturedType);
                Hide();
            });
            _buttons.Add(btn);
        }

        // 위치 설정 (마우스 우측 하단으로 살짝 오프셋)
        transform.position = position + offset;
        
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
