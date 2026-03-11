using Mirror;
using UnityEngine;
using System;

public class LobbyNetworkPlayer : NetworkBehaviour
{
    public static LobbyNetworkPlayer Local =>
        NetworkClient.localPlayer?.GetComponent<LobbyNetworkPlayer>();

    [SyncVar] public int userId;
    [SyncVar] public string nickname;
    [SyncVar] public int level;

    public static event Action<LobbyNetworkPlayer> OnPlayerReady;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log("Local Network Player Ready");
        OnPlayerReady?.Invoke(this);
    }

    [Command]
    public void CmdRequestMatch()
    {
        CustomNetworkManager.Instance.RequestMatch(this);
    }

    [Server]
    public void SetInfo(MyAuthenticator.AuthRequestMessage authData)
    {
        userId = authData.userId;
        nickname = authData.nickname;
        level = authData.level;
    }

    // 서버 → 특정 클라이언트에게만 배틀서버 정보 전송
    [TargetRpc]
    public void TargetMoveToServer(NetworkConnectionToClient conn, string ip, ushort port)
    {
        Debug.Log($"[CLIENT] Moving to battle server {ip}:{port}");
        CustomNetworkManager.Instance.StartBattleConnect(ip, port);
    }
}