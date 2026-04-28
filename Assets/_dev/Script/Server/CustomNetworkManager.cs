using Mirror;
using Newtonsoft.Json.Linq;
using System;
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

        var authData = (MyAuthenticator.AuthRequestMessage)conn.authenticationData;

        if (serverType == "lobby")
        {
            // 스폰 전에 SyncVar 세팅 → 초기 스폰 메시지에 올바른 값이 포함됨
            var lobbyPlayer = player.GetComponent<LobbyNetworkPlayer>();
            lobbyPlayer?.SetInfo(authData);
            NetworkServer.AddPlayerForConnection(conn, player);
            Debug.Log($"[LOBBY] 입장: {authData.nickname}");
        }
        else if (serverType == "battle")
        {
            // 스폰 전에 SyncVar 세팅 → OnStartServer/OnStartClient에서 올바른 값 사용
            var battlePlayer = player.GetComponent<BattleNetworkPlayer>();
            battlePlayer?.SetInfo(authData);
            NetworkServer.AddPlayerForConnection(conn, player);

            Debug.Log($"[BATTLE] 입장: {authData.nickname} | 캐릭터: {authData.selectedCharacter}");

            // StartBattle은 RegisterPlayer(PlayerStats)가 2명 채워질 때 자동 호출됨
            // BattleTimer 코루틴은 BattleManager.Update()가 대신 처리하므로 중복 불필요
        }
    }

    [Server]
    public void RequestMatch(LobbyNetworkPlayer player)
    {
        if (matchQueue.Contains(player)) return;
        matchQueue.Add(player);
        Debug.Log($"[MATCH] 대기 {matchQueue.Count}명");

        if (matchQueue.Count >= 2)
        {
            Debug.Log("[MATCH] 2명 확보, 매칭을 시작합니다.");
            StartMatch();
        }
    }

    [Server]
    public void CancelMatch(LobbyNetworkPlayer player)
    {
        if (matchQueue.Remove(player))
            Debug.Log($"[MATCH] 취소: {player.nickname}, 남은 대기 {matchQueue.Count}명");
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


    // 매치 종료 후 모든 플레이어가 나갔을 때 BattleManager가 호출
    // 포트를 백엔드에 반환 → 다음 매칭에서 이 포트를 다시 배분 가능
    [Server]
    public async void ReleaseMatchPort()
    {
        int port = GetComponent<kcp2k.KcpTransport>().port;
        await BackendManager.ReleasePort(port);
        Debug.Log($"[BATTLE] 포트 {port} 반환 완료 — 다음 매치 가능");
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
