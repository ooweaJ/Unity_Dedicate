using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance;

    private List<NetworkConnectionToClient> matchQueue = new();

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
            Debug.Log("Starting Dedicated Server");
            StartServer();
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        Debug.Log("Player joined server");
    }

    // 클라이언트 → 서버 매칭 요청
    [Server]
    public void RequestMatch(NetworkConnectionToClient conn)
    {
        Debug.Log($"[MATCH] request from connId={conn.connectionId}");

        if (matchQueue.Contains(conn))
        {
            Debug.Log("[MATCH] already queued");
            return;
        }

        matchQueue.Add(conn);
        Debug.Log($"[MATCH] queue count = {matchQueue.Count}");

        if (matchQueue.Count >= 2)
        {
            Debug.Log("[MATCH] START");
            StartMatch();
        }
    }

    [Server]
    void StartMatch()
    {
        Debug.Log("Match Found!");

        ServerChangeScene("BattleScene");
    }
}
