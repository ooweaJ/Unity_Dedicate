using Mirror;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 종료 팝업
///
/// 인스펙터 연결:
///   resultPanel      — 루트 패널 오브젝트 (평소 비활성)
///   titleText        — "승리!" / "패배" / "무승부"
///   winnerText       — "○○ 승리"
///   expValueText     — "+150 EXP"
///   rankValueText    — "+20" / "-15"
///   scoreContainer   — ScoreRow들의 부모 (VerticalLayoutGroup)
///   scoreRowPrefab   — TextMeshProUGUI 5개: [이름, 결과, 킬, 딜, 경험치]
///   timerText        — "10초 후 자동 이동"
///   confirmButton    — "로비로" 버튼
/// </summary>
public class BattleResultUI : MonoBehaviour
{
    public static BattleResultUI Instance { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject resultPanel;

    [Header("결과 타이틀")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI winnerText;

    [Header("내 경험치 / 랭크")]
    [SerializeField] private TextMeshProUGUI expValueText;
    [SerializeField] private TextMeshProUGUI rankValueText;

    [Header("스코어보드")]
    [SerializeField] private Transform  scoreContainer;
    [SerializeField] private GameObject scoreRowPrefab;

    [Header("하단")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button          confirmButton;

    private static readonly Color ColorVictory = new Color(1f, 0.84f, 0f);
    private static readonly Color ColorDefeat  = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color ColorDraw    = new Color(0.7f, 0.7f, 0.7f);

    private Action _onConfirm;
    private float  _autoTimer;
    private bool   _active;

    private void Awake()
    {
        Instance = this;
        resultPanel?.SetActive(false);
        confirmButton?.onClick.AddListener(Confirm);
    }

    public void Show(
        string winnerName, bool isDraw,
        uint[] netIds, string[] names,
        int[] kills, int[] deaths, float[] damages,
        bool[] isWinner, int[] exps, int[] rankDeltas,
        float autoReturnTime, Action onConfirm)
    {
        if (resultPanel == null)
        {
            Debug.LogError("[RESULT UI] resultPanel이 연결되지 않음 — 인스펙터 확인");
            return;
        }

        _onConfirm = onConfirm;
        _autoTimer = autoReturnTime;
        _active    = true;

        resultPanel.SetActive(true);
        Debug.Log("[RESULT UI] 결과창 표시");

        // 로컬 플레이어를 netId로 찾기 (gameObject.name 비교 X — 모두 "Player(Clone)")
        uint localNetId   = NetworkClient.localPlayer?.netId ?? 0;
        int  localIdx     = FindLocalIndex(netIds, localNetId);
        bool localIsWinner = localIdx >= 0 && isWinner[localIdx];

        SetTitle(winnerName, isDraw, localIsWinner);
        SetLocalPlayerExp(localIdx, exps, rankDeltas);
        BuildScoreRows(names, kills, damages, isWinner, exps);
    }

    // ─── 타이틀 ────────────────────────────────────────────────────────
    private void SetTitle(string winnerName, bool isDraw, bool localIsWinner)
    {
        if (isDraw)
        {
            titleText.text  = "무승부";
            titleText.color = ColorDraw;
            if (winnerText != null) winnerText.text = "무승부";
        }
        else if (localIsWinner)
        {
            titleText.text  = "승리!";
            titleText.color = ColorVictory;
            if (winnerText != null) winnerText.text = $"{winnerName} 승리";
        }
        else
        {
            titleText.text  = "패배";
            titleText.color = ColorDefeat;
            if (winnerText != null) winnerText.text = $"{winnerName} 승리";
        }
    }

    // ─── 로컬 플레이어 EXP / 랭크 ─────────────────────────────────────
    private void SetLocalPlayerExp(int localIdx, int[] exps, int[] rankDeltas)
    {
        if (localIdx < 0) return;

        if (expValueText != null)
            expValueText.text = $"+{exps[localIdx]} EXP";

        if (rankValueText != null)
        {
            int rd = rankDeltas[localIdx];
            rankValueText.text  = rd >= 0 ? $"+{rd}" : rd.ToString();
            rankValueText.color = rd >= 0 ? ColorVictory : ColorDefeat;
        }
    }

    // ─── 스코어보드 행 생성 ────────────────────────────────────────────
    // ScoreRowPrefab 컬럼 순서: [이름] [결과] [킬] [딜] [경험치]
    private void BuildScoreRows(
        string[] names, int[] kills, float[] damages,
        bool[] isWinner, int[] exps)
    {
        foreach (Transform child in scoreContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < names.Length; i++)
        {
            var row   = Instantiate(scoreRowPrefab, scoreContainer);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length < 5)
            {
                Debug.LogWarning($"[RESULT UI] ScoreRowPrefab에 TextMeshProUGUI가 {texts.Length}개뿐 — 5개 필요");
                continue;
            }

            texts[0].text = names[i];
            texts[1].text = isWinner[i] ? "승" : "패";
            texts[2].text = kills[i].ToString();
            texts[3].text = $"{damages[i]:F0}";
            texts[4].text = $"+{exps[i]}";

            if (isWinner[i]) texts[0].color = ColorVictory;
        }
    }

    // ─── 카운트다운 ───────────────────────────────────────────────────
    private void Update()
    {
        if (!_active) return;

        _autoTimer -= Time.deltaTime;

        if (timerText != null)
            timerText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, _autoTimer))}초 후 자동 이동";

        if (_autoTimer <= 0f)
            Confirm();
    }

    private void Confirm()
    {
        if (!_active) return;
        _active = false;
        _onConfirm?.Invoke();
    }

    // ─── 유틸 ─────────────────────────────────────────────────────────
    private static int FindLocalIndex(uint[] netIds, uint localNetId)
    {
        if (localNetId == 0) return -1;
        for (int i = 0; i < netIds.Length; i++)
            if (netIds[i] == localNetId) return i;
        return -1;
    }
}
