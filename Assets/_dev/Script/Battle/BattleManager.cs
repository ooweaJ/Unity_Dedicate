// BattleManager.cs — 결과창 + 비동기 씬 전환
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : NetworkBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float resultDisplayTime = 8f;
    [SerializeField] private string lobbySceneName   = "LobbyScene";
    [SerializeField] private int    requiredPlayers   = 2;

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
        resultTracker[player] = new PlayerResultData { playerName = player.gameObject.name };

        if (players.Count >= requiredPlayers)
            StartBattle();
    }

    [Server]
    public void UnregisterPlayer(PlayerStats player)
    {
        players.Remove(player);
        resultTracker.Remove(player);
    }

    // ─── 시작 ──────────────────────────────────────────────────────────
    [Server]
    public void StartBattle()
    {
        if (currentState != BattleState.WaitingForPlayers) return;
        currentState = BattleState.InProgress;
        battleTimer = 180f;
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
        if (currentState != BattleState.InProgress) return;
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
        var names = result.playerResults.Select(p => p.playerName).ToArray();
        var kills = result.playerResults.Select(p => p.kills).ToArray();
        var deaths = result.playerResults.Select(p => p.deaths).ToArray();
        var damages = result.playerResults.Select(p => p.damageDealt).ToArray();
        var isWinner = result.playerResults.Select(p => p.isWinner).ToArray();

        RpcShowResultAndPreload(
            isDraw ? "" : winner?.gameObject.name ?? "",
            isDraw,
            names, kills, deaths, damages, isWinner
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
            kvp.Value.isWinner = !isDraw && kvp.Key == winner;
            result.playerResults.Add(kvp.Value);
        }
        return result;
    }

    // ─── ClientRpc: 결과창 표시 + 로비 비동기 사전 로딩 ─────────────────
    // 발로란트와 동일한 방식:
    // 1. 결과창 즉시 표시 (배틀씬에서)
    // 2. 백그라운드에서 로비씬 미리 로딩 (LoadSceneAsync)
    // 3. 플레이어가 버튼 누르면 → 이미 로딩된 씬 즉시 활성화
    [ClientRpc]
    private void RpcShowResultAndPreload(
        string winnerName, bool isDraw,
        string[] names, int[] kills, int[] deaths,
        float[] damages, bool[] isWinner)
    {
        // 결과창 표시
        BattleResultUI.Instance?.Show(
            winnerName, isDraw,
            names, kills, deaths, damages, isWinner,
            onConfirm: () => ActivateLobby()  // 버튼 콜백
        );

        // 백그라운드에서 로비씬 미리 로딩 시작
        // allowSceneActivation = false → 로딩만 하고 전환은 안 함
        StartCoroutine(PreloadLobbyScene());
    }

    private AsyncOperation _lobbyLoadOperation;

    private IEnumerator PreloadLobbyScene()
    {
        _lobbyLoadOperation = SceneManager.LoadSceneAsync(lobbySceneName);
        _lobbyLoadOperation.allowSceneActivation = false; // 로딩만, 전환 X

        // 90%까지 로딩됨 (Unity는 allowSceneActivation=false일 때 90%에서 멈춤)
        while (_lobbyLoadOperation.progress < 0.9f)
            yield return null;

        Debug.Log("[BATTLE] 로비씬 사전 로딩 완료 — 버튼 대기 중");

        // 자동 전환 옵션: resultDisplayTime 후 자동으로 넘어가게 하려면
        yield return new WaitForSeconds(resultDisplayTime);
        ActivateLobby(); // 시간 초과 시 자동 전환
    }

    // 버튼 누르거나 시간 초과 시 호출
    public void ActivateLobby()
    {
        if (_lobbyLoadOperation == null) return;

        // Mirror 서버 연결 해제 (클라이언트)
        if (!isServer) NetworkClient.Disconnect();

        // 이미 90% 로딩된 씬 즉시 활성화 → 로딩 없는 것처럼 보임
        _lobbyLoadOperation.allowSceneActivation = true;
        _lobbyLoadOperation = null;
    }

    private void OnBattleStateChanged(BattleState _, BattleState newState)
    {
        Debug.Log($"[BATTLE] 상태: {newState}");
    }
}