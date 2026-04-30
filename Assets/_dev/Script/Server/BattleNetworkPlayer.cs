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
    [SyncVar] public CharacterType selectedCharacter;
    [SyncVar] public int           teamId;

    // 최종 스탯 동기화 (SyncVar로 모든 클라이언트가 알 수 있음)
    [SyncVar] public CharacterStatData stats;

    // 매치마다 리셋 — 매치 시작 전 서버에서 호출
    private static int s_teamCounter;
    public static void ResetTeamCounter() => s_teamCounter = 0;

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
        Debug.Log($"[BATTLE] 로컬 플레이어 준비 | 캐릭터: {selectedCharacter} | ATK: {stats.atk}");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        players.Remove(this);
    }

    public void SetInfo(MyAuthenticator.AuthRequestMessage authData)
    {
        userId            = authData.userId;
        nickname          = authData.nickname;
        nicknameHook      = authData.nickname;
        level             = authData.level;
        selectedCharacter = authData.selectedCharacter;
        stats             = authData.stats;
        teamId            = s_teamCounter++;
    }

    [TargetRpc]
    public void TargetReturnToLobby(NetworkConnectionToClient conn)
    {
        CustomNetworkManager.Instance.ReturnToLobby();
    }
}
