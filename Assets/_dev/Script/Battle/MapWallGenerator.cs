using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class MapWallGenerator : MonoBehaviour
{
    [Header("맵 크기 (MapFloorGenerator 와 동일하게 입력)")]
    [SerializeField] [Min(1)] private int width  = 10;
    [SerializeField] [Min(1)] private int height = 10;

    [Header("벽 설정")]
    [SerializeField] private GameObject[] wallPrefabs;
    [SerializeField] [Min(1)] private int wallHeight = 3;

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            if (Application.isPlaying) return;
            if (!gameObject.scene.IsValid()) return;
            Generate();
        };
    }
#endif

    private void Awake()
    {
        if (Application.isPlaying)
            Generate();
    }

    [ContextMenu("Generate Walls")]
    public void Generate()
    {
        ClearWalls();
        SpawnWalls();
    }

    // ── 벽 생성 ───────────────────────────────────────────────────────────────

    private void SpawnWalls()
    {
        if (wallPrefabs == null || wallPrefabs.Length == 0) return;

        float offsetX = (width  + 1) * 0.5f;
        float offsetZ = (height + 1) * 0.5f;

        // Front (z+) / Back (z-) — 코너 포함 (width+2)
        var front = CreateContainer("Wall_Front");
        var back  = CreateContainer("Wall_Back");

        for (int x = 0; x < width + 2; x++)
        {
            float wx = x - offsetX;
            SpawnColumn(front, wx,  offsetZ, x);
            SpawnColumn(back,  wx, -offsetZ, x);
        }

        // Left (x-) / Right (x+) — 코너 제외 (height)
        var left  = CreateContainer("Wall_Left");
        var right = CreateContainer("Wall_Right");

        for (int z = 0; z < height; z++)
        {
            float wz = z - (height - 1) * 0.5f;
            SpawnColumn(left,  -offsetX, wz, z);
            SpawnColumn(right,  offsetX, wz, z);
        }
    }

    private void SpawnColumn(Transform container, float wx, float wz, int colIndex)
    {
        for (int y = 0; y < wallHeight; y++)
        {
            var wall = SpawnRandom(wallPrefabs, container);
            if (wall == null) continue;
            wall.name = $"Wall_{colIndex}_y{y}";
            wall.transform.localPosition = new Vector3(wx, y, wz);
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale    = Vector3.one;
        }
    }

    // ── 정리 ──────────────────────────────────────────────────────────────────

    private void ClearWalls()
    {
        string[] groups = { "Wall_Front", "Wall_Back", "Wall_Left", "Wall_Right" };
        foreach (var groupName in groups)
        {
            var existing = transform.Find(groupName);
            if (existing == null) continue;

            if (Application.isPlaying) Destroy(existing.gameObject);
            else                       DestroyImmediate(existing.gameObject);
        }
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────────

    private Transform CreateContainer(string containerName)
    {
        var go = new GameObject(containerName);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        return go.transform;
    }

    private static GameObject SpawnRandom(GameObject[] prefabs, Transform parent)
    {
        var prefab = prefabs[Random.Range(0, prefabs.Length)];
        return prefab == null ? null : Instantiate(prefab, parent);
    }
}
