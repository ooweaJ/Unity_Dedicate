using Mirror;
using UnityEngine;
using System;

public class LobbyNetworkPlayer : NetworkBehaviour
{
    public static LobbyNetworkPlayer Local
    {
        get
        {
            return NetworkClient.localPlayer.GetComponent<LobbyNetworkPlayer>();
        }
    }

    [SyncVar] public int userId;
    [SyncVar] public string nickname;
    [SyncVar] public int level;

    // UI���� ���� ���� �̺�Ʈ
    public static event Action<LobbyNetworkPlayer> OnPlayerReady;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        Debug.Log("Local Network Player Ready");

        string myNickname = PlayerDataManager.Instance.GetUsername();
    }

    [Command]
    public void CmdRequestMatch()
    {
        CustomNetworkManager.Instance.RequestMatch(connectionToClient);
    }

    public void CmdSetPlayerInfo()
    {
        userId = PlayerDataManager.Instance.GetUserId();
        nickname = PlayerDataManager.Instance.GetUsername();
        level = PlayerDataManager.Instance.GetLevel();
    }

    [Server]
    public void SetInfo(MyAuthenticator.AuthRequestMessage authData)
    {
        userId = authData.userId;
        nickname = authData.nickname;
        level = authData.level;
    }
}
