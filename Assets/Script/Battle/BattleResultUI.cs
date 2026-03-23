// BattleResultUI.cs
using Mirror;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultUI : MonoBehaviour
{
    public static BattleResultUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI titleText;       // "승리!" / "패배" / "무승부"
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Transform scoreContainer;
    [SerializeField] private GameObject scoreRowPrefab;
    [SerializeField] private Button confirmButton;   // "로비로" 버튼
    [SerializeField] private TextMeshProUGUI timerText;       // 자동 전환 카운트다운

    private Action _onConfirm;
    private float _autoTimer = 8f;
    private bool _confirmed = false;

    private void Awake()
    {
        Instance = this;
        resultPanel?.SetActive(false);
        confirmButton?.onClick.AddListener(OnConfirmClicked);
    }

    public void Show(
        string winnerName, bool isDraw,
        string[] names, int[] kills, int[] deaths,
        float[] damages, bool[] isWinner,
        Action onConfirm)
    {
        _onConfirm = onConfirm;
        _confirmed = false;
        _autoTimer = 8f;

        resultPanel?.SetActive(true);

        // 로컬 플레이어 기준으로 타이틀 결정
        string localName = NetworkClient.localPlayer?.gameObject.name ?? "";
        if (isDraw)
            titleText.text = "무승부";
        else if (winnerName == localName)
            titleText.text = "승리!";
        else
            titleText.text = "패배";

        winnerText.text = isDraw ? "무승부" : $"{winnerName} 승리";

        // 기존 행 제거
        foreach (Transform child in scoreContainer)
            Destroy(child.gameObject);

        // 플레이어별 결과 행 생성
        for (int i = 0; i < names.Length; i++)
        {
            var row = Instantiate(scoreRowPrefab, scoreContainer);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 5)
            {
                texts[0].text = names[i];
                texts[1].text = isWinner[i] ? "승" : "패";
                texts[2].text = kills[i].ToString();
                texts[3].text = deaths[i].ToString();
                texts[4].text = $"{damages[i]:F0}";
            }
        }
    }

    private void Update()
    {
        if (!resultPanel.activeSelf || _confirmed) return;

        // 자동 전환 카운트다운
        _autoTimer -= Time.deltaTime;
        if (timerText != null)
            timerText.text = $"{Mathf.CeilToInt(_autoTimer)}초 후 자동 이동";

        // 시간 초과는 BattleManager 코루틴에서 처리
    }

    private void OnConfirmClicked()
    {
        if (_confirmed) return;
        _confirmed = true;
        _onConfirm?.Invoke(); // BattleManager.ActivateLobby() 호출
    }
}
