using Mirror;
using UnityEngine;
using System;

public class LobbyNetworkPlayer : NetworkBehaviour
{
    public static LobbyNetworkPlayer Local;

    [SyncVar] public int userId;
    [SyncVar] public string nickname;
    [SyncVar] public int level;

    // UI���� ���� ���� �̺�Ʈ
    public static event Action<LobbyNetworkPlayer> OnPlayerReady;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        Debug.Log("Local Network Player Ready");
        Local = this;

        string myNickname = PlayerDataManager.Instance.GetUsername();

        // ������ �� �г��� ���
        CmdSetPlayerInfo();
    }

    [Command]
    public void CmdRequestMatch()
    {
        CustomNetworkManager.Instance.RequestMatch(connectionToClient);
    }

    [Command]
    public void CmdSetPlayerInfo()
    {
        userId = PlayerDataManager.Instance.GetUserId();
        nickname = PlayerDataManager.Instance.GetUsername();
        level = PlayerDataManager.Instance.GetLevel();
    }
}
