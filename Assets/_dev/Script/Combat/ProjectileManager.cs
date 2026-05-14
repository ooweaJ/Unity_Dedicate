using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체 behavior 프리팹 관리 + 외형/FX 카탈로그
/// 배틀 씬에 오브젝트 하나 만들고 이 컴포넌트 추가 후 인스펙터에서 연결
/// </summary>
public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [Header("Behavior 프리팹 (컴포넌트만, 외형 없음)")]
    [SerializeField] private GameObject basicPrefab;
    [SerializeField] private GameObject explosivePrefab;
    [SerializeField] private GameObject bouncingPrefab;

    [Header("외형 프리팹 카탈로그 (순서 = 인덱스)")]
    [SerializeField] private GameObject[] visuals;

    [Header("FX 프리팹 카탈로그 (순서 = 인덱스)")]
    [Tooltip("SO의 hitEffectPrefab / wallHitEffectPrefab 에 연결한 프리팹을 여기에도 등록.\n" +
             "등록 안 하면 인덱스 -1 → 경고 로그 발생")]
    [SerializeField] private GameObject[] fxPrefabs;

    private Dictionary<GameObject, int> _visualIndexMap;
    private Dictionary<GameObject, int> _fxIndexMap;

    private void Awake()
    {
        Instance = this;
        BuildIndex(_visualIndexMap = new(), visuals);
        BuildIndex(_fxIndexMap    = new(), fxPrefabs);
    }

    private static void BuildIndex(Dictionary<GameObject, int> map, GameObject[] arr)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null)
                map[arr[i]] = i;
    }

    // ─── Behavior ─────────────────────────────────────────────────────────
    public GameObject GetBehaviorPrefab(ProjectileType type)
    {
        switch (type)
        {
            case ProjectileType.Basic:     return basicPrefab;
            case ProjectileType.Explosive: return explosivePrefab;
            case ProjectileType.Bouncing:  return bouncingPrefab;
            default:
                Debug.LogError($"[ProjectileManager] 미등록 타입: {type}");
                return null;
        }
    }

    // ─── 외형 비주얼 ───────────────────────────────────────────────────────
    public int GetVisualIndex(GameObject prefab)
    {
        if (prefab == null) return -1;
        if (_visualIndexMap.TryGetValue(prefab, out int i)) return i;
        Debug.LogWarning($"[ProjectileManager] 외형 프리팹 '{prefab.name}' visuals[] 미등록");
        return -1;
    }

    public GameObject GetVisual(int index)
    {
        if (visuals == null || index < 0 || index >= visuals.Length) return null;
        return visuals[index];
    }

    // ─── FX (히트 / 벽충돌 공용) ──────────────────────────────────────────
    public int GetFxIndex(GameObject prefab)
    {
        if (prefab == null) return -1;
        if (_fxIndexMap.TryGetValue(prefab, out int i)) return i;
        Debug.LogWarning($"[ProjectileManager] FX 프리팹 '{prefab.name}' fxPrefabs[] 미등록");
        return -1;
    }

    public GameObject GetFxPrefab(int index)
    {
        if (fxPrefabs == null || index < 0 || index >= fxPrefabs.Length) return null;
        return fxPrefabs[index];
    }
}
