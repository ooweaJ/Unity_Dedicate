using Mirror;
using Newtonsoft.Json.Linq;
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
///   goldValueText    — "+200 G"
///   rankValueText    — "+20" / "-15"
///   scoreContainer   — ScoreRow들의 부모 (VerticalLayoutGroup)
///   scoreRowPrefab   — TextMeshProUGUI 5개: [이름, 결과, 킬, 딜, 골드]
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

    [Header("내 골드 / 랭크")]
    [SerializeField] private TextMeshProUGUI goldValueText;
    [SerializeField] private TextMeshProUGUI rankValueText;

    [Header("스코어보드")]
    [SerializeField] private Transform  scoreContainer;
    [SerializeField] private GameObject scoreRowPrefab;
    [Tooltip("팀 구분선 프리팹 (선택). TextMeshProUGUI 1개로 팀 이름 표시. null이면 구분선 생략")]
    [SerializeField] private GameObject teamHeaderPrefab;

    [Header("하단")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button          confirmButton;

    private static readonly Color ColorVictory = new Color(1f, 0.84f, 0f);
    private static readonly Color ColorDefeat  = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color ColorDraw    = new Color(0.7f, 0.7f, 0.7f);

    private static readonly Color RowColorAlly  = new Color(0.15f, 0.35f, 0.75f, 0.85f);
    private static readonly Color RowColorEnemy = new Color(0.75f, 0.15f, 0.15f, 0.85f);

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
        bool[] isWinner, int[] exps, int[] golds, int[] rankDeltas,
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

        uint localNetId    = NetworkClient.localPlayer?.netId ?? 0;
        int  localIdx      = FindLocalIndex(netIds, localNetId);
        bool localIsWinner = localIdx >= 0 && isWinner[localIdx];

        int localTeamId = BattleNetworkPlayer.Local?.teamId ?? -1;
        int[] teamIds   = ResolveTeamIds(netIds);

        SetTitle(isDraw, localIsWinner);
        SetLocalPlayerGold(localIdx, golds, rankDeltas);
        BuildScoreRows(names, kills, damages, isWinner, golds, teamIds, localTeamId);

        if (localIdx >= 0)
            ReportBattleToServer(exps[localIdx], golds[localIdx]);
    }

    private static int[] ResolveTeamIds(uint[] netIds)
    {
        var result = new int[netIds.Length];
        for (int i = 0; i < netIds.Length; i++)
        {
            var bnp = BattleNetworkPlayer.players.Find(p => p.netId == netIds[i]);
            result[i] = bnp?.teamId ?? -1;
        }
        return result;
    }

    private async void ReportBattleToServer(int gainedExp, int gainedGold)
    {
        try
        {
            int    userId = PlayerDataManager.Instance.GetUserId();
            string json   = await BackendManager.BattleResult(userId, gainedExp, gainedGold);

            var response = JObject.Parse(json);
            if (response["success"] is JToken s && (bool)s)
            {
                if (response["user"] is JToken user)
                    PlayerDataManager.Instance.ApplyUserData(user);
            }
            else
            {
                Debug.LogWarning($"[BattleResult] 서버 오류: {response["message"]}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BattleResult] API 호출 실패: {e.Message}");
        }
    }

    // ─── 타이틀 ────────────────────────────────────────────────────────
    private void SetTitle(bool isDraw, bool localIsWinner)
    {
        if (isDraw)
        {
            titleText.text  = "무승부";
            titleText.color = ColorDraw;
        }
        else if (localIsWinner)
        {
            titleText.text  = "승리!";
            titleText.color = ColorVictory;
        }
        else
        {
            titleText.text  = "패배";
            titleText.color = ColorDefeat;
        }

        // 승리자 텍스트 미사용
        if (winnerText != null) winnerText.gameObject.SetActive(false);
    }

    // ─── 로컬 플레이어 골드 / 랭크 ────────────────────────────────────
    private void SetLocalPlayerGold(int localIdx, int[] golds, int[] rankDeltas)
    {
        if (localIdx < 0) return;

        if (goldValueText != null)
            goldValueText.text = $"+{golds[localIdx]} G";

        if (rankValueText != null)
        {
            int rd = rankDeltas[localIdx];
            rankValueText.text  = rd >= 0 ? $"+{rd}" : rd.ToString();
            rankValueText.color = rd >= 0 ? ColorVictory : ColorDefeat;
        }
    }

    // ─── 스코어보드 행 생성 ────────────────────────────────────────────
    // ScoreRowPrefab 컬럼 순서: [이름] [결과] [킬] [딜] [골드]
    private void BuildScoreRows(
        string[] names, int[] kills, float[] damages,
        bool[] isWinner, int[] golds, int[] teamIds, int localTeamId)
    {
        foreach (Transform child in scoreContainer)
            Destroy(child.gameObject);

        var sorted = new System.Collections.Generic.List<int>(names.Length);
        for (int i = 0; i < names.Length; i++) sorted.Add(i);
        sorted.Sort((a, b) =>
        {
            // 내 팀 먼저, 같은 팀 안에서는 킬 많은 순
            int teamA = teamIds[a] == localTeamId ? 0 : 1;
            int teamB = teamIds[b] == localTeamId ? 0 : 1;
            if (teamA != teamB) return teamA.CompareTo(teamB);
            return kills[b].CompareTo(kills[a]);
        });

        int lastGroup = -2;
        foreach (int i in sorted)
        {
            int group = teamIds[i] == localTeamId ? 0 : 1;

            // 팀 경계: 헤더 행 삽입
            if (group != lastGroup)
            {
                if (teamHeaderPrefab != null)
                {
                    var header = Instantiate(teamHeaderPrefab, scoreContainer);
                    var ht = header.GetComponentInChildren<TextMeshProUGUI>();
                    if (ht != null) ht.text = group == 0 ? "내 팀" : "상대 팀";
                }
                lastGroup = group;
            }

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
            texts[4].text = $"+{golds[i]}G";

            // 텍스트 전체 흰색
            foreach (var t in texts) t.color = Color.white;

            // 행 배경: 아군 파란색 / 적 빨간색
            var bg = row.GetComponent<UnityEngine.UI.Image>();
            if (bg != null) bg.color = group == 0 ? RowColorAlly : RowColorEnemy;
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
