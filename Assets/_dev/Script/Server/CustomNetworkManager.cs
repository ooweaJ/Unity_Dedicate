using Mirror;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance;
    public string serverType = "lobby";
    public bool   isMovingToBattle = false;

    public event Action OnClientConnected;

    [Header("Player Prefabs")]
    public GameObject lobbyPlayerPrefab;
    public GameObject battlePlayerPrefab;

    [Header("Character Prefabs")]
    [Tooltip("CharacterType enum 순서대로 캐릭터 외형 프리팹 연결\n[0]=Swordsman [1]=Mage ...")]
    public GameObject[] characterPrefabs;

    private List<LobbyNetworkPlayer> matchQueue = new();
    public bool blockSceneActivation = false;

    public override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public override void Start()
    {
        base.Start();
        if (Application.isBatchMode)
        {
            ParseCommandLineArgs();
            StartServer();
        }
    }

    private void ParseCommandLineArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-port" && i + 1 < args.Length)
            {
                if (ushort.TryParse(args[i + 1], out ushort port))
                {
                    GetComponent<kcp2k.KcpTransport>().port = port;
                    Debug.Log($"[SERVER] Port: {port}");
                }
            }
            if (args[i] == "-serverType" && i + 1 < args.Length)
            {
                serverType = args[i + 1];
                Debug.Log($"[SERVER] ServerType: {serverType}");
            }
            if (args[i] == "-scene" && i + 1 < args.Length)
            {
                onlineScene = args[i + 1];
                Debug.Log($"[SERVER] Scene: {onlineScene}");
            }
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[SERVER] OnServerAddPlayer | serverType={serverType}");

        GameObject prefab = serverType == "battle" ? battlePlayerPrefab : lobbyPlayerPrefab;
        GameObject player = Instantiate(prefab);

        if (SpawnManager.Instance != null)
            player.transform.position = SpawnManager.Instance.GetNextSpawnPosition();

        NetworkServer.AddPlayerForConnection(conn, player);

        var authData = (MyAuthenticator.AuthRequestMessage)conn.authenticationData;

        if (serverType == "lobby")
        {
            var lobbyPlayer = player.GetComponent<LobbyNetworkPlayer>();
            if (lobbyPlayer != null)
            {
                lobbyPlayer.SetInfo(authData);
                Debug.Log($"[LOBBY] 입장: {authData.nickname}");
            }
        }
        else if (serverType == "battle")
        {
            var battlePlayer = player.GetComponent<BattleNetworkPlayer>();
            if (battlePlayer != null)
            {
                // 1. BattleNetworkPlayer에 유저 정보 저장
                battlePlayer.SetInfo(authData);

                Debug.Log($"[BATTLE] 입장: {authData.nickname} | 캐릭터: {authData.selectedCharacter}");

                if (numPlayers >= 2)
                {
                    Debug.Log("[BATTLE] 2명 모두 접속! 배틀 시작");
                    BattleManager.Instance?.StartBattle();
                    StartCoroutine(BattleTimer());
                }
            }
        }
    }

    [Server]
    public void RequestMatch(LobbyNetworkPlayer player)
    {
        if (matchQueue.Contains(player)) return;
        matchQueue.Add(player);
        Debug.Log($"[MATCH] 대기 {matchQueue.Count}명");
        if (matchQueue.Count >= 2) StartMatch();
    }

    [Server]
    private async void StartMatch()
    {
        var matched = new List<LobbyNetworkPlayer> { matchQueue[0], matchQueue[1] };
        matchQueue.RemoveRange(0, 2);

        string res  = await BackendManager.AcquirePort();
        JObject json = JObject.Parse(res);

        if (json["success"].ToObject<bool>())
        {
            int port = json["port"].ToObject<int>();
            Debug.Log($"[MATCH] 포트 확보: {port}");
            foreach (var p in matched)
                p.TargetMoveToServer(p.connectionToClient, "127.0.0.1", (ushort)port);
        }
        else
        {
            Debug.LogWarning("[MATCH] 서버 없음");
        }
    }

    public override void OnClientDisconnect()
    {
        if (isMovingToBattle) { isMovingToBattle = false; return; }
    }

    public override void OnStopClient()
    {
        if (isMovingToBattle) return;
        base.OnStopClient();
    }

    public void StartBattleConnect(string ip, ushort port)
    {
        isMovingToBattle = true;
        StopClient();
        SceneFlowManager.Instance.Load(new LoadRequest
        {
            sceneName     = "BattleScene",
            serverAddress = "127.0.0.1",
            port          = 7778
        });
    }

    [Server]
    public IEnumerator BattleTimer()
    {
        Debug.Log("[BATTLE] 타이머 시작");
        yield return new WaitForSeconds(180f);
        EndBattle();
    }

    [Server]
    private void EndBattle()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            var player = conn.identity?.GetComponent<BattleNetworkPlayer>();
            if (player != null) player.TargetReturnToLobby(conn);
        }
        ShutdownBattleServer();
    }

    private async void ShutdownBattleServer()
    {
        await Task.Delay(3000);
        int port = GetComponent<kcp2k.KcpTransport>().port;
        await BackendManager.ReleasePort(port);
        Debug.Log($"[BATTLE] 포트 {port} 반환");
    }

    public void ReturnToLobby()
    {
        Debug.Log("[CLIENT] 로비 복귀");
        isMovingToBattle = true;
        StopClient();
        SceneFlowManager.Instance.Load(new LoadRequest
        {
            sceneName     = "MainLobbyScene",
            serverAddress = "127.0.0.1",
            port          = 7777
        });
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        OnClientConnected?.Invoke();
    }
}
