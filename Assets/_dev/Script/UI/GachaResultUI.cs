// GachaResultUI.cs
// 가챠씬에 붙이는 결과 UI

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GachaResultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Transform cardContainer;  // 카드 나열할 부모
    [SerializeField] private GachaResultCard cardPrefab;     // 카드 프리팹

    private List<GachaResultCard> _spawnedCards = new();

    public void Show(GachaRewardItem[] results)
    {
        // 기존 카드 제거
        foreach (var card in _spawnedCards)
            Destroy(card.gameObject);
        _spawnedCards.Clear();

        // 결과마다 카드 생성
        foreach (var item in results)
        {
            var data = GachaRewardDatabase.Instance.Find(item.typeId, item.rewardId);
            if (data == null) continue;

            var card = Instantiate(cardPrefab, cardContainer);
            card.Setup(data, item.grade, item.resultType);
            _spawnedCards.Add(card);
        }

        resultPanel.SetActive(true);
        StartCoroutine(RevealCards());
    }

    // 카드 순서대로 등장 연출
    IEnumerator RevealCards()
    {
        foreach (var card in _spawnedCards)
        {
            card.PlayReveal();
            yield return new WaitForSeconds(0.15f);  // 카드 간 딜레이
        }
    }

    public void Hide()
    {
        resultPanel.SetActive(false);
    }
}