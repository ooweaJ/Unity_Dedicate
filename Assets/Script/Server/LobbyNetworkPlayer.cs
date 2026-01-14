using Mirror;
using UnityEngine;
using System;

public class LobbyNetworkPlayer : NetworkBehaviour
{
    public static LobbyNetworkPlayer Local;

    [SyncVar] public int userId;
    [SyncVar] public string nickname;
    [SyncVar] public int level;

    // UI에서 쓰기 위한 이벤트
    public static event Action<LobbyNetworkPlayer> OnPlayerReady;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        Debug.Log("Local Network Player Ready");
        Local = this;

        string myNickname = PlayerDataManager.Instance.GetUsername();

        // 서버에 내 닉네임 등록
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
