using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance;
    public string serverType = "lobby";
    public bool isMovingToBattle = false;

    [Header("Player Prefabs")]
    public GameObject lobbyPlayerPrefab;
    public GameObject battlePlayerPrefab;

    private Queue<ushort> availablePorts = new Queue<ushort>(new ushort[] { 7778, 7779, 7780 });
    private HashSet<ushort> inUsePorts = new HashSet<ushort>();
    private List<LobbyNetworkPlayer> matchQueue = new();

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
    void StartMatch()
    {
        // 사용 가능한 포트 가져오기
        if (availablePorts.Count == 0)
        {
            Debug.LogWarning("[MATCH] No available battle servers!");
            // TODO: 클라이언트에게 대기 알림
            return;
        }

        ushort port = availablePorts.Dequeue();
        inUsePorts.Add(port);
        Debug.Log($"[MATCH] Match found! Assigning port {port}");

        // 매칭된 2명 추출
        var matched = new List<LobbyNetworkPlayer>
        {
            matchQueue[0],
            matchQueue[1]
        };
        matchQueue.RemoveRange(0, 2);

        // 2명에게만 배틀서버로 이동 명령
        foreach (var player in matched)
        {
            player.TargetMoveToServer(player.connectionToClient, "127.0.0.1", port);
        }
    }

    [Server]
    public void ReleasePort(ushort port)
    {
        if (inUsePorts.Remove(port))
        {
            availablePorts.Enqueue(port);
            Debug.Log($"[SERVER] Port {port} released");
        }
    }
    public override void OnClientDisconnect()
    {
        if (isMovingToBattle)
        {
            Debug.Log($"두번체크");
            isMovingToBattle = false;
            return; // 배틀서버 이동중이면 기본동작 무시
            
        }
        base.OnClientDisconnect(); // 일반 disconnect면 오프라인씬 이동
    }

    public override void OnStopClient()
    {
        if (isMovingToBattle) return;

        base.OnStopClient();
    }

    public void StartBattleConnect(string ip, ushort port)
    {
        isMovingToBattle = true;
        StartCoroutine(ConnectWithDelay(ip, port, 2f));
    }

    private System.Collections.IEnumerator ConnectWithDelay(string ip, ushort port, float delay)
    {
        StopClient();
        Debug.Log($"[CLIENT] Waiting {delay}s for battle server...");
        yield return new WaitForSeconds(delay);

        var transport = GetComponent<kcp2k.KcpTransport>();
        transport.port = port;
        networkAddress = ip;
        StartClient();
        Debug.Log($"[CLIENT] Connecting to {ip}:{port}");
    }
}