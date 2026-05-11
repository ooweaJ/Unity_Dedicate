using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class MapFloorGenerator : MonoBehaviour
{
    private const string FloorContainer = "Floor";

    [Header("맵 크기 (타일 단위, 타일 1개 = 1×1)")]
    [SerializeField] [Min(1)] private int width  = 10;
    [SerializeField] [Min(1)] private int height = 10;

    [Header("바닥 타일 프리팹 목록 (랜덤 선택)")]
    [SerializeField] private GameObject[] tilePrefabs;

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

    [ContextMenu("Generate Floor")]
    public void Generate()
    {
        ClearFloor();
        SpawnFloor();
    }

    private void SpawnFloor()
    {
        if (tilePrefabs == null || tilePrefabs.Length == 0) return;

        var container = CreateContainer(FloorContainer);

        // 바닥 전체를 덮는 단일 콜리전 (타일 개수와 무관하게 1개)
        var col = container.gameObject.AddComponent<BoxCollider>();
        col.center = Vector3.zero;
        col.size   = new Vector3(width, 1f, height);

        float offsetX = (width  - 1) * 0.5f;
        float offsetZ = (height - 1) * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var tile = SpawnRandom(tilePrefabs, container);
                if (tile == null) continue;
                tile.name = $"Tile_{x}_{z}";
                tile.transform.localPosition = new Vector3(x - offsetX, 0f, z - offsetZ);
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale    = Vector3.one;
            }
        }
    }

    private void ClearFloor()
    {
        var existing = transform.Find(FloorContainer);
        if (existing == null) return;

        if (Application.isPlaying) Destroy(existing.gameObject);
        else                       DestroyImmediate(existing.gameObject);
    }

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
