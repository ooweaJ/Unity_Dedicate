// BattleManager.cs — 결과창 + 비동기 씬 전환
using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : NetworkBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float resultDisplayTime = 10f;
    [SerializeField] private int   requiredPlayers   = 4;

    public enum BattleState { WaitingForPlayers, InProgress, Ended }

    [SyncVar(hook = nameof(OnBattleStateChanged))]
    private BattleState currentState = BattleState.WaitingForPlayers;

    private readonly List<PlayerStats> players = new();
    private readonly Dictionary<PlayerStats, PlayerResultData> resultTracker = new();
    private IScoreService scoreService;
    private float battleTimer = 180f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        scoreService = new MockScoreService();
    }

    // ─── 등록 ──────────────────────────────────────────────────────────
    [Server]
    public void RegisterPlayer(PlayerStats player)
    {
        if (players.Contains(player)) return;
        players.Add(player);
        var bnp = player.GetComponent<BattleNetworkPlayer>();
        resultTracker[player] = new PlayerResultData
        {
            netId      = player.netId,
            playerName = bnp?.nickname ?? player.gameObject.name
        };

        if (players.Count >= requiredPlayers)
            StartBattle();
    }

    [Server]
    public void UnregisterPlayer(PlayerStats player)
    {
        players.Remove(player);
        resultTracker.Remove(player);

        // 모든 플레이어가 나갔고 매치가 끝난 상태 → 다음 매치를 위해 리셋
        if (players.Count == 0 && currentState == BattleState.Ended)
        {
            currentState = BattleState.WaitingForPlayers;
            BattleNetworkPlayer.ResetTeamCounter();
            SpawnManager.Instance?.ResetSpawnCounts();
            CustomNetworkManager.Instance.ReleaseMatchPort();
            Debug.Log("[BATTLE] 상태 리셋 — 다음 매치 대기");
        }
    }

    // ─── 시작 ──────────────────────────────────────────────────────────
    [Server]
    public void StartBattle()
    {
        if (currentState != BattleState.WaitingForPlayers)
        {
            Debug.LogWarning($"[BATTLE] StartBattle 무시 — currentState={currentState} (이전 매치 서버가 재사용되고 있을 가능성)");
            return;
        }
        currentState = BattleState.InProgress;
        battleTimer  = 180f;
        Debug.Log("[BATTLE] 배틀 시작");
    }

    private void Update()
    {
        if (!isServer || currentState != BattleState.InProgress) return;
        battleTimer -= Time.deltaTime;
        if (battleTimer <= 0f) EndByTimeLimit();
    }

    // ─── 전적 기록 ─────────────────────────────────────────────────────
    [Server]
    public void RecordDamage(PlayerStats attacker, float damage)
    {
        if (resultTracker.TryGetValue(attacker, out var d)) d.damageDealt += damage;
    }

    [Server]
    public void RecordKill(PlayerStats killer, PlayerStats victim)
    {
        if (resultTracker.TryGetValue(killer, out var k)) k.kills++;
        if (resultTracker.TryGetValue(victim, out var v)) v.deaths++;
    }

    // ─── 승패 판정 ─────────────────────────────────────────────────────
    [Server]
    public void OnPlayerDead(PlayerStats dead)
    {
        if (currentState != BattleState.InProgress)
        {
            Debug.LogWarning($"[BATTLE] OnPlayerDead 무시 — currentState={currentState}");
            return;
        }
        var alive = players.Where(p => p != null && !p.IsDead).ToList();

        if (alive.Count == 0) { EndBattle(-1, isDraw: true); return; }

        // 살아있는 팀이 1개뿐이면 그 팀 승리
        var aliveTeams = alive.Select(GetTeamId).Distinct().ToList();
        if (aliveTeams.Count == 1)
            EndBattle(aliveTeams[0], isDraw: false);
    }

    [Server]
    private void EndByTimeLimit()
    {
        var alive = players.Where(p => !p.IsDead).ToList();

        if (alive.Count == 0) { EndBattle(-1, isDraw: true); return; }

        // 팀별 HP 비율 합산 → 높은 팀 승리
        var teamHP  = alive.GroupBy(GetTeamId)
                           .ToDictionary(g => g.Key, g => g.Sum(p => p.CurrentHpRatio));
        var ordered = teamHP.OrderByDescending(kv => kv.Value).ToList();
        bool isDraw = ordered.Count > 1 && Mathf.Abs(ordered[0].Value - ordered[1].Value) < 0.01f;
        EndBattle(isDraw ? -1 : ordered[0].Key, isDraw);
    }

    [Server]
    private void EndBattle(int winnerTeamId, bool isDraw)
    {
        if (currentState == BattleState.Ended) return;
        currentState = BattleState.Ended;

        var result = BuildResult(winnerTeamId, isDraw);

        // 백엔드 API 호출 (점수 반영) — 서버에서만, await 안 기다림
        _ = scoreService.ReportMatchResult(result);

        // 직렬화 가능한 배열로 변환해서 RPC 전송
        var netIds     = result.playerResults.Select(p => p.netId).ToArray();
        var names      = result.playerResults.Select(p => p.playerName).ToArray();
        var kills      = result.playerResults.Select(p => p.kills).ToArray();
        var deaths     = result.playerResults.Select(p => p.deaths).ToArray();
        var damages    = result.playerResults.Select(p => p.damageDealt).ToArray();
        var isWinner   = result.playerResults.Select(p => p.isWinner).ToArray();
        var exps       = result.playerResults.Select(p => p.expGained).ToArray();
        var rankDeltas = result.playerResults.Select(p => p.rankPointDelta).ToArray();

        string winnerNickname = isDraw ? "" :
            string.Join(" & ", resultTracker
                .Where(kv => kv.Value.isWinner)
                .Select(kv => kv.Value.playerName));

        RpcShowResultAndPreload(
            winnerNickname, isDraw,
            netIds, names, kills, deaths, damages, isWinner,
            exps, rankDeltas, resultDisplayTime
        );
    }

    [Server]
    private BattleResultData BuildResult(int winnerTeamId, bool isDraw)
    {
        var result = new BattleResultData
        {
            winnerName = isDraw ? "무승부" : $"Team {winnerTeamId}",
            isDraw     = isDraw
        };
        foreach (var kvp in resultTracker)
        {
            kvp.Value.isWinner       = !isDraw && GetTeamId(kvp.Key) == winnerTeamId;
            kvp.Value.expGained      = ExpCalculator.Calculate(kvp.Value, isDraw);
            kvp.Value.rankPointDelta = ExpCalculator.RankDelta(kvp.Value.isWinner, isDraw);
            result.playerResults.Add(kvp.Value);
        }
        return result;
    }

    private int GetTeamId(PlayerStats ps) =>
        ps.GetComponent<BattleNetworkPlayer>()?.teamId ?? -1;

    // ─── ClientRpc: 결과창 표시 ─────────────────────────────────────────
    [ClientRpc]
    private void RpcShowResultAndPreload(
        string winnerName, bool isDraw,
        uint[] netIds, string[] names, int[] kills, int[] deaths,
        float[] damages, bool[] isWinner,
        int[] exps, int[] rankDeltas, float autoReturnTime)
    {
        if (BattleResultUI.Instance == null)
        {
            Debug.LogError("[BATTLE] BattleResultUI.Instance가 null — 씬에 BattleResultUI 컴포넌트가 없음");
            return;
        }

        BattleResultUI.Instance.Show(
            winnerName, isDraw,
            netIds, names, kills, deaths, damages, isWinner,
            exps, rankDeltas, autoReturnTime,
            onConfirm: () => ActivateLobby()
        );

    }

    // 버튼 누르거나 타이머 만료 시 호출
    // ReturnToLobby()가 배틀서버 disconnect + 로비서버 재연결을 모두 처리
    public void ActivateLobby()
    {
        CustomNetworkManager.Instance.ReturnToLobby();
    }

    private void OnBattleStateChanged(BattleState _, BattleState newState)
    {
        Debug.Log($"[BATTLE] 상태: {newState}");
    }
}