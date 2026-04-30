using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// 부쉬 영역 컴포넌트.
/// 플레이어 진입/이탈을 서버에서 감지하고 팀별 시야(teamVisionMask)를 동기화한다.
/// </summary>
public class BushZone : NetworkBehaviour
{
    // 팀별 시야 비트마스크 — teamId N의 팀이 이 부쉬에 있으면 N번 비트 ON
    [SyncVar(hook = nameof(OnMaskChanged))]
    public int teamVisionMask;

    // 서버 전용: bushState → teamId 매핑
    private readonly Dictionary<PlayerBushState, int> _playerTeams = new();

    // 클라이언트: 마스크가 바뀔 때 이 부쉬 안 플레이어들이 구독해서 가시성 재계산
    public event Action OnVisionMaskChanged;

    public bool HasTeamVision(int teamId) => (teamVisionMask & (1 << teamId)) != 0;

    // ── 트리거 ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        var bushState = other.transform.root.GetComponent<PlayerBushState>();
        if (bushState == null || _playerTeams.ContainsKey(bushState)) return;

        var netPlayer = other.transform.root.GetComponent<BattleNetworkPlayer>();
        int team      = netPlayer != null ? netPlayer.teamId : -1;

        _playerTeams[bushState] = team;

        if (team >= 0)
            teamVisionMask |= (1 << team);

        bushState.EnterBush(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        var bushState = other.transform.root.GetComponent<PlayerBushState>();
        if (bushState == null || !_playerTeams.ContainsKey(bushState)) return;

        int team = _playerTeams[bushState];
        _playerTeams.Remove(bushState);

        if (team >= 0 && !HasAnyPlayerOfTeam(team))
            teamVisionMask &= ~(1 << team);

        bushState.ExitBush();
    }

    private bool HasAnyPlayerOfTeam(int teamId)
    {
        foreach (var kv in _playerTeams)
            if (kv.Value == teamId) return true;
        return false;
    }

    // SyncVar hook — 클라이언트에서 마스크 변경 이벤트 발행
    private void OnMaskChanged(int _, int __)
    {
        OnVisionMaskChanged?.Invoke();
    }
}
