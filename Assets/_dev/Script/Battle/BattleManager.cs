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
    [SerializeField] private int   requiredPlayers   = 2;

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
        if (alive.Count == 1) EndBattle(alive[0], isDraw: false);
        else if (alive.Count == 0) EndBattle(null, isDraw: true);
    }

    [Server]
    private void EndByTimeLimit()
    {
        var winner = players.Where(p => !p.IsDead)
                            .OrderByDescending(p => p.CurrentHpRatio)
                            .FirstOrDefault();
        EndBattle(winner, isDraw: winner == null);
    }

    [Server]
    private void EndBattle(PlayerStats winner, bool isDraw)
    {
        if (currentState == BattleState.Ended) return;
        currentState = BattleState.Ended;

        var result = BuildResult(winner, isDraw);

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
            (winner != null ? resultTracker[winner].playerName : "");

        RpcShowResultAndPreload(
            winnerNickname, isDraw,
            netIds, names, kills, deaths, damages, isWinner,
            exps, rankDeltas, resultDisplayTime
        );
    }

    [Server]
    private BattleResultData BuildResult(PlayerStats winner, bool isDraw)
    {
        var result = new BattleResultData
        {
            winnerName = isDraw ? "" : winner?.gameObject.name ?? "",
            isDraw = isDraw
        };
        foreach (var kvp in resultTracker)
        {
            kvp.Value.isWinner       = !isDraw && kvp.Key == winner;
            kvp.Value.expGained      = ExpCalculator.Calculate(kvp.Value, isDraw);
            kvp.Value.rankPointDelta = ExpCalculator.RankDelta(kvp.Value.isWinner, isDraw);
            result.playerResults.Add(kvp.Value);
        }
        return result;
    }

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