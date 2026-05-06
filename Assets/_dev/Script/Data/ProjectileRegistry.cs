using UnityEngine;

/// <summary>
/// 투사체 behavior 프리팹 + 외형 프리팹 카탈로그
///
/// 사용법:
///   1. Project 우클릭 → Create → Game/Projectile Registry 로 에셋 생성
///   2. behaviors 슬롯에 Basic/Explosive/Bouncing 각 behavior 프리팹 연결
///   3. visuals 배열에 외형 프리팹 등록 (순서가 인덱스가 됨)
///   4. 씬 내 오브젝트(예: GameDataManager)에서 이 SO를 [SerializeField]로 참조해야 OnEnable이 호출됨
/// </summary>
[CreateAssetMenu(fileName = "ProjectileRegistry", menuName = "Game/Projectile Registry")]
public class ProjectileRegistry : ScriptableObject
{
    public static ProjectileRegistry Instance { get; private set; }

    [System.Serializable]
    public class BehaviorEntry
    {
        public ProjectileType type;
        [Tooltip("해당 타입의 컴포넌트가 붙은 behavior 프리팹")]
        public GameObject prefab;
    }

    [Header("Behavior 프리팹 (타입별)")]
    public BehaviorEntry[] behaviors;

    [Header("외형 프리팹 카탈로그 (순서 = 인덱스)")]
    public GameObject[] visuals;

    private void OnEnable() => Instance = this;

    public GameObject GetBehaviorPrefab(ProjectileType type)
    {
        if (behaviors == null) return null;
        foreach (var e in behaviors)
            if (e.type == type) return e.prefab;
        Debug.LogError($"[ProjectileRegistry] {type} behavior 프리팹 미등록");
        return null;
    }

    public int GetVisualIndex(GameObject visualPrefab)
    {
        if (visuals == null || visualPrefab == null) return -1;
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] == visualPrefab) return i;
        return -1;
    }

    public GameObject GetVisual(int index)
    {
        if (visuals == null || index < 0 || index >= visuals.Length) return null;
        return visuals[index];
    }
}
