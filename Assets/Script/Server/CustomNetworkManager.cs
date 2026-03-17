using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance;
    public string serverType = "lobby";
    public bool isMovingToBattle = false;

    public event Action OnClientConnected;

    [Header("Player Prefabs")]
    public GameObject lobbyPlayerPrefab;
    public GameObject battlePlayerPrefab;

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

    void ParseCommandLineArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-port" && i + 1 < args.Length)
            {
                if (ushort.TryParse(args[i + 1], out ushort port))
                {
                    var transport = GetComponent<kcp2k.KcpTransport>();
                    transport.port = port;
                    Debug.Log($"[SERVER] Port set to {port}");
                }
            }
            if (args[i] == "-serverType" && i + 1 < args.Length)
            {
                serverType = args[i + 1];
                Debug.Log($"[SERVER] ServerType set to {serverType}");
            }
            if (args[i] == "-scene" && i + 1 < args.Length)
            {
                onlineScene = args[i + 1];
                Debug.Log($"[SERVER] Scene set to {onlineScene}");
            }
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[SERVER] OnServerAddPlayer called, serverType={serverType}");

        GameObject prefab = serverType == "battle"
            ? battlePlayerPrefab
            : lobbyPlayerPrefab;

        GameObject player = Instantiate(prefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        var authData = (MyAuthenticator.AuthRequestMessage)conn.authenticationData;

        if (serverType == "lobby")
        {
            var lobbyPlayer = player.GetComponent<LobbyNetworkPlayer>();
            if (lobbyPlayer != null)
            {
                lobbyPlayer.SetInfo(authData);
                Debug.Log($"[LOBBY] Player joined: {authData.nickname}");
            }
        }
        else if (serverType == "battle")
        {
            var battlePlayer = player.GetComponent<BattleNetworkPlayer>();
            if (battlePlayer != null)
            {
                battlePlayer.SetInfo(authData);
                Debug.Log($"[BATTLE] Player joined: {authData.nickname}");

                if (numPlayers >= 2)
                {
                    Debug.Log("[BATTLE] 2명 모두 접속! 타이머 시작");
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
        Debug.Log($"[MATCH] Queue count = {matchQueue.Count}");

        if (matchQueue.Count >= 2)
            StartMatch();
    }

    [Server]
    async void StartMatch()
    {
        var matched = new List<LobbyNetworkPlayer>
        {
            matchQueue[0],
            matchQueue[1]
        };
        matchQueue.RemoveRange(0, 2);

        string res = await BackendManager.AcquirePort();
        JObject json = JObject.Parse(res);

        if (json["success"].ToObject<bool>())
        {
            int port = json["port"].ToObject<int>();
            Debug.Log($"[MATCH] Port acquired: {port}");
            foreach (var player in matched)
                player.TargetMoveToServer(player.connectionToClient, "127.0.0.1", (ushort)port);
        }
        else
        {
            Debug.LogWarning("[MATCH] No available servers!");
            // TODO: 클라이언트에게 대기 알림
        }
    }

    public override void OnClientDisconnect()
    {
        if (isMovingToBattle)
        {
            isMovingToBattle = false;
            return;
        }
        LobbyController.Instance.HandleLogout();
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
            sceneName = "BattleScene",
            serverAddress = "127.0.0.1",
            port = 7778
        });
    }

    [Server]
    public IEnumerator BattleTimer()
    {
        Debug.Log("[BATTLE] 10초 후 종료");
        yield return new WaitForSeconds(10f);
        EndBattle();
    }

    [Server]
    void EndBattle()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            var player = conn.identity?.GetComponent<BattleNetworkPlayer>();
            if (player != null)
                player.TargetReturnToLobby(conn);
        }
        ShutdownBattleServer();
    }

    async void ShutdownBattleServer()
    {
        await Task.Delay(3000);
        int port = GetComponent<kcp2k.KcpTransport>().port;
        await BackendManager.ReleasePort(port);
        Debug.Log($"[BATTLE] Port {port} released, waiting for next match");
    }

    public void ReturnToLobby()
    {
        Debug.Log("[CLIENT] 로비 복귀 시작");
        isMovingToBattle = true;
        StopClient();
        SceneFlowManager.Instance.Load(new LoadRequest
        {
            sceneName = "MainLobbyScene",
            serverAddress = "127.0.0.1",
            port = 7777
        });
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        OnClientConnected?.Invoke();
    }
}