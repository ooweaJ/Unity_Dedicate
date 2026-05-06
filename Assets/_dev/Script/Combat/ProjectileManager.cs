using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체 behavior 프리팹 관리 + 외형 카탈로그
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

    // 인스펙터용 배열 → 런타임 Dictionary (O(1) 조회)
    private Dictionary<GameObject, int> _visualIndexMap;

    private void Awake()
    {
        Instance = this;
        BuildVisualIndex();
    }

    private void BuildVisualIndex()
    {
        _visualIndexMap = new Dictionary<GameObject, int>(visuals?.Length ?? 0);
        if (visuals == null) return;
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null)
                _visualIndexMap[visuals[i]] = i;
    }

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

    public int GetVisualIndex(GameObject visualPrefab)
    {
        if (visualPrefab == null) return -1;
        if (_visualIndexMap.TryGetValue(visualPrefab, out int index)) return index;
        Debug.LogWarning($"[ProjectileManager] 외형 프리팹 '{visualPrefab.name}' 카탈로그 미등록");
        return -1;
    }

    public GameObject GetVisual(int index)
    {
        if (visuals == null || index < 0 || index >= visuals.Length) return null;
        return visuals[index];
    }
}
