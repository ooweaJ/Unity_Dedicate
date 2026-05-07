using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// 부쉬 영역 컴포넌트.
/// 플레이어 진입/이탈을 서버에서 감지하고 팀별 시야(teamVisionMask)를 동기화한다.
/// 로컬 플레이어가 진입하면 부쉬 메쉬를 반투명으로 전환한다.
/// </summary>
public class BushZone : NetworkBehaviour
{
    // 팀별 시야 비트마스크 — teamId N의 팀이 이 부쉬에 있으면 N번 비트 ON
    [SyncVar(hook = nameof(OnMaskChanged))]
    public int teamVisionMask;

    [Header("Visual")]
    [Tooltip("비워두면 자식 Renderer를 자동 탐색합니다.")]
    [SerializeField] private Renderer[] bushMeshRenderers;
    [SerializeField] [Range(0f, 1f)] private float bushFadeAlpha = 0.35f;

    // 서버 전용: bushState → teamId 매핑
    private readonly Dictionary<PlayerBushState, int> _playerTeams = new();

    // 클라이언트: 마스크가 바뀔 때 이 부쉬 안 플레이어들이 구독해서 가시성 재계산
    public event Action OnVisionMaskChanged;

    private Material[][] _bushOriginalMats;
    private Material[][] _bushFadeMats;

    public bool HasTeamVision(int teamId) => (teamVisionMask & (1 << teamId)) != 0;

    // ── 초기화 ───────────────────────────────────────────────────────────────

    private void Start()
    {
        if (bushMeshRenderers == null || bushMeshRenderers.Length == 0)
            bushMeshRenderers = GetComponentsInChildren<Renderer>();

        _bushOriginalMats = new Material[bushMeshRenderers.Length][];
        _bushFadeMats     = new Material[bushMeshRenderers.Length][];

        for (int i = 0; i < bushMeshRenderers.Length; i++)
        {
            _bushOriginalMats[i] = bushMeshRenderers[i].sharedMaterials;
            _bushFadeMats[i]     = PlayerBushState.CreateFadeMaterialArray(_bushOriginalMats[i], bushFadeAlpha);
        }
    }

    private void OnDestroy()
    {
        if (_bushFadeMats != null)
            foreach (var arr in _bushFadeMats)
                if (arr != null)
                    foreach (var mat in arr)
                        if (mat != null) Destroy(mat);
    }

    // ── 트리거 ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // 클라이언트: 로컬 플레이어 진입 시 부쉬 즉시 반투명 (서버 딜레이 없이)
        if (isClient)
        {
            var localBushState = other.transform.root.GetComponent<PlayerBushState>();
            if (localBushState != null && localBushState.isLocalPlayer)
                SetLocalPlayerInside(true);
        }

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
        // 클라이언트: 로컬 플레이어 이탈 시 즉시 복원
        if (isClient)
        {
            var localBushState = other.transform.root.GetComponent<PlayerBushState>();
            if (localBushState != null && localBushState.isLocalPlayer)
                SetLocalPlayerInside(false);
        }

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

    // ── 부쉬 메쉬 반투명 (로컬 플레이어가 진입/이탈 시 호출) ──────────────────

    public void SetLocalPlayerInside(bool inside)
    {
        Debug.Log($"[BushDebug] SetLocalPlayerInside | inside={inside} renderers={bushMeshRenderers?.Length ?? 0} fadeMats={(  _bushFadeMats != null ? "ready" : "null")}");
        if (bushMeshRenderers == null || _bushFadeMats == null) return;

        for (int i = 0; i < bushMeshRenderers.Length; i++)
        {
            var r = bushMeshRenderers[i];
            if (r == null) continue;

            if (inside) r.materials      = _bushFadeMats[i];
            else        r.sharedMaterials = _bushOriginalMats[i];
        }
    }
}
