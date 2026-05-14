using Mirror;
using UnityEngine;

/// <summary>
/// 스폰 포인트 관리
/// 배틀씬에 SpawnPoint 오브젝트들을 등록해서 플레이어를 분산 스폰
/// </summary>
public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    [Header("Spawn Points")]
    [Tooltip("배틀씬에 배치할 스폰 포인트들 (Inspector에서 등록)")]
    public Transform[] spawnPoints;

    [Header("Team Spawn Offset")]
    [Tooltip("팀 내 플레이어 간 X축 간격 (2명이면 -0.5x, +0.5x)")]
    public float teamSpawnSpread = 1.5f;

    private int spawnIndex = 0;
    private readonly int[] _teamSlot = new int[2]; // 팀별 스폰 슬롯 인덱스

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 팀 ID 기반 스폰 위치 반환 — spawnPoints[teamId]를 기준으로 X축 spread
    /// </summary>
    public Vector3 GetSpawnPositionForTeam(int teamId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[SPAWN] 스폰 포인트 없음! (0, 0, 0) 반환");
            return Vector3.zero;
        }

        int pointIndex = Mathf.Clamp(teamId, 0, spawnPoints.Length - 1);
        Vector3 basePos = spawnPoints[pointIndex].position;

        // 팀 내 슬롯 0 → -spread, 슬롯 1 → +spread
        int slot = _teamSlot[teamId % 2]++;
        float xOffset = slot == 0 ? -teamSpawnSpread : teamSpawnSpread;
        return basePos + new Vector3(xOffset, 0f, 0f);
    }

    /// <summary>
    /// 매치 종료 후 다음 매치를 위해 슬롯 리셋
    /// </summary>
    public void ResetSpawnCounts()
    {
        _teamSlot[0] = 0;
        _teamSlot[1] = 0;
        spawnIndex   = 0;
    }

    /// <summary>
    /// 다음 스폰 포인트 위치 반환 (순환) — 팀 무관 레거시 용도
    /// </summary>
    public Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[SPAWN] 스폰 포인트 없음! (0, 0, 0) 반환");
            return Vector3.zero;
        }

        Vector3 pos = spawnPoints[spawnIndex % spawnPoints.Length].position;
        spawnIndex++;
        return pos;
    }

    /// <summary>
    /// 플레이어를 스폰 포인트로 순간이동
    /// </summary>
    [Server]
    public void RespawnPlayer(NetworkConnectionToClient conn)
    {
        if (conn.identity == null) return;

        Vector3 spawnPos = GetNextSpawnPosition();
        conn.identity.transform.position = spawnPos;
        Debug.Log($"[SPAWN] 플레이어 {conn.connectionId} → {spawnPos}");
    }
}
