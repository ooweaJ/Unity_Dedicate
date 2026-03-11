using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System;

public class BattleNetworkPlayer : NetworkBehaviour
{
    public static BattleNetworkPlayer Local =>
        NetworkClient.localPlayer?.GetComponent<BattleNetworkPlayer>();

    public static List<BattleNetworkPlayer> players = new List<BattleNetworkPlayer>();
    public static event Action<BattleNetworkPlayer> OnPlayerJoined;

    [SyncVar] public int userId;
    [SyncVar(hook = nameof(OnNicknameChanged))] public string nickname;
    [SyncVar] public int level;

    void OnNicknameChanged(string oldValue, string newValue)
    {
        OnPlayerJoined?.Invoke(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        players.Add(this);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log("[BATTLE] Local Battle Player Ready");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        players.Remove(this);
    }

    void OnDestroy()
    {
        players.Clear();
    }

    [Server]
    public void SetInfo(MyAuthenticator.AuthRequestMessage authData)
    {
        userId = authData.userId;
        nickname = authData.nickname;
        level = authData.level;
    }
}