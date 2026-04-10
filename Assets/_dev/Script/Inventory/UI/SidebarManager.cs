using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SidebarManager : MonoBehaviour
{
    [SerializeField] private List<TabButton> tabs;
    [SerializeField] private RectTransform indicator;
    [SerializeField] private Image indicatorImage;
    [SerializeField] private float moveSpeed = 15f;

    private Color richGold = new Color(1f, 0.75f, 0.03f);
    private Vector2 _targetPos;
    private int _currentTabId = -1;

    // 탭이 바뀌었을 때 UIManager에게 알려줄 이벤트
    public Action<int> OnTabChanged;

    public void Init()
    {
        foreach (var tab in tabs)
        {
            // 각 버튼에 클릭 시 내부 함수 호출 바인딩
            tab.OnTabClicked = HandleTabClicked;
        }

        // 초기 탭 설정 (0번 상세 정보)
        if (tabs.Count > 0) HandleTabClicked(tabs[0]);
    }

    private void HandleTabClicked(TabButton selected)
    {
        if (_currentTabId == selected.tabId) return;
        _currentTabId = selected.tabId;

        // 1. 모든 탭 순회하며 색상 리셋
        foreach (var tab in tabs)
        {
            bool isTarget = (tab == selected);
            tab.SetSelect(isTarget, richGold);
        }

        // 2. 인디케이터(막대기)도 황금색으로 세트 맞춤
        //if (indicatorImage != null) indicatorImage.color = richGold;

        // 3. 인디케이터 이동 목표 설정
        RectTransform rt = selected.GetComponent<RectTransform>();
        _targetPos = new Vector2(indicator.anchoredPosition.x, rt.anchoredPosition.y);

        OnTabChanged?.Invoke(_currentTabId);
    }

    void Update()
    {
        // 인디케이터의 부드러운 이동은 여기서 전담
        indicator.anchoredPosition = Vector2.Lerp(
            indicator.anchoredPosition,
            _targetPos,
            Time.deltaTime * moveSpeed
        );
    }
}