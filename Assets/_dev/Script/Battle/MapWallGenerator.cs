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

    [Header("레이어")]
    [Tooltip("Wall 컨테이너에 적용할 레이어 이름 (Project Settings > Tags & Layers)")]
    [SerializeField] private string wallLayerName = "Wall";

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

        float offsetX    = (width  + 1) * 0.5f;
        float offsetZ    = (height + 1) * 0.5f;
        float colCenterY = (wallHeight - 1) * 0.5f;

        int wallLayer = LayerMask.NameToLayer(wallLayerName);
        if (wallLayer < 0) wallLayer = 0;

        // Front (z+) / Back (z-) — 코너 포함 (width+2)
        var front = CreateContainer("Wall_Front", wallLayer);
        AddCollider(front, new Vector3(0f, colCenterY,  offsetZ), new Vector3(width + 2, wallHeight, 1f));

        var back = CreateContainer("Wall_Back", wallLayer);
        AddCollider(back,  new Vector3(0f, colCenterY, -offsetZ), new Vector3(width + 2, wallHeight, 1f));

        for (int x = 0; x < width + 2; x++)
        {
            float wx = x - offsetX;
            SpawnColumn(front, wx,  offsetZ, x);
            SpawnColumn(back,  wx, -offsetZ, x);
        }

        // Left (x-) / Right (x+) — 코너 제외 (height)
        var left = CreateContainer("Wall_Left", wallLayer);
        AddCollider(left,  new Vector3(-offsetX, colCenterY, 0f), new Vector3(1f, wallHeight, height));

        var right = CreateContainer("Wall_Right", wallLayer);
        AddCollider(right, new Vector3( offsetX, colCenterY, 0f), new Vector3(1f, wallHeight, height));

        for (int z = 0; z < height; z++)
        {
            float wz = z - (height - 1) * 0.5f;
            SpawnColumn(left,  -offsetX, wz, z);
            SpawnColumn(right,  offsetX, wz, z);
        }
    }

    private static void AddCollider(Transform container, Vector3 center, Vector3 size)
    {
        var col = container.gameObject.AddComponent<BoxCollider>();
        col.center = center;
        col.size   = size;
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

    private Transform CreateContainer(string containerName, int layer = 0)
    {
        var go = new GameObject(containerName);
        go.layer = layer;
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
