using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GachaResultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Transform cardContainer;   // HorizontalLayoutGroup
    [SerializeField] private GachaResultCard cardPrefab;

    public event Action ConfirmClicked;

    private List<GachaResultCard> _cards = new();

    public void Setup()
    {
        var results = GachaContext.PendingResults;
        if (results == null) return;

        // 기존 카드 제거
        foreach (var card in _cards)
            Destroy(card.gameObject);
        _cards.Clear();

        // 결과마다 카드 생성
        foreach (var item in results)
        {
            var card = Instantiate(cardPrefab, cardContainer);
            card.Setup(item);   // typeId + rewardId → SO 내부에서 찾음
            _cards.Add(card);
        }
    }

    public void open() { resultPanel.SetActive(true); }
    public void close() { resultPanel.SetActive(false); }

    public void OnConfirmClicked()
    {
        ConfirmClicked.Invoke();
    }
}