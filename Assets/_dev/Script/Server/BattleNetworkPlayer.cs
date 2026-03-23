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

    [SyncVar] public int           userId;
    [SyncVar] public string        nickname;
    [SyncVar] public int           level;
    [SyncVar] public CharacterType selectedCharacter;   // 로비에서 고른 캐릭터

    [SyncVar(hook = nameof(OnNicknameChanged))]
    public string nicknameHook;

    private void OnNicknameChanged(string _, string newValue)
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
        Debug.Log($"[BATTLE] 로컬 플레이어 준비 | 캐릭터: {selectedCharacter}");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        players.Remove(this);
    }

    private void OnDestroy()
    {
        players.Clear();
    }

    [Server]
    public void SetInfo(MyAuthenticator.AuthRequestMessage authData)
    {
        userId            = authData.userId;
        nickname          = authData.nickname;
        nicknameHook      = authData.nickname;
        level             = authData.level;
        selectedCharacter = authData.selectedCharacter;
    }

    [TargetRpc]
    public void TargetReturnToLobby(NetworkConnectionToClient conn)
    {
        Debug.Log("[CLIENT] 로비 복귀 명령 받음");
        CustomNetworkManager.Instance.ReturnToLobby();
    }
}
