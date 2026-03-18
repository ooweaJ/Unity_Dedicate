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

    private int spawnIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 다음 스폰 포인트 위치 반환 (순환)
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
