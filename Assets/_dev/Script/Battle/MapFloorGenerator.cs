using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class MapFloorGenerator : MonoBehaviour
{
    private const string TilePrefix = "Tile_";
    private const string WallPrefix = "Wall_";

    [Header("맵 크기 (타일 단위, 타일 1개 = 1×1)")]
    [SerializeField] [Min(1)] private int width  = 10;
    [SerializeField] [Min(1)] private int height = 10;

    [Header("바닥 타일 프리팹 목록 (랜덤 선택)")]
    [SerializeField] private GameObject[] tilePrefabs;

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

    [ContextMenu("Generate Floor")]
    public void Generate()
    {
        ClearAll();
        SpawnFloor();
        SpawnWalls();
    }

    // ── 바닥 ─────────────────────────────────────────────────────────────────

    private void SpawnFloor()
    {
        if (tilePrefabs == null || tilePrefabs.Length == 0) return;

        float offsetX = (width  - 1) * 0.5f;
        float offsetZ = (height - 1) * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var tile = SpawnRandom(tilePrefabs, transform);
                if (tile == null) continue;
                tile.name = $"{TilePrefix}{x}_{z}";
                tile.transform.localPosition = new Vector3(x - offsetX, 0f, z - offsetZ);
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale    = Vector3.one;
            }
        }
    }

    // ── 벽 (4면 + 코너 포함) ─────────────────────────────────────────────────
    //
    //  Front/Back 벽이 코너까지 포함 (width+2 길이)
    //  Left/Right 벽은 코너 제외 (height 길이)
    //
    //       [F F F F F F F F F F F F]   ← Front (width+2)
    //       [L]  . . . . . . . . [R]
    //       [L]  . . . . . . . . [R]
    //       [B B B B B B B B B B B B]   ← Back  (width+2)

    private void SpawnWalls()
    {
        if (wallPrefabs == null || wallPrefabs.Length == 0) return;

        float offsetX = (width  + 1) * 0.5f;
        float offsetZ = (height + 1) * 0.5f;

        // Front (z+), Back (z-): width+2 길이 (코너 포함)
        for (int x = 0; x < width + 2; x++)
        {
            float wx = x - offsetX;        // -(width/2) ~ +(width/2)
            SpawnWallColumn($"Front_{x}", wx,  offsetZ);
            SpawnWallColumn($"Back_{x}",  wx, -offsetZ);
        }

        // Left (x-), Right (x+): height 길이 (코너 제외)
        for (int z = 0; z < height; z++)
        {
            float wz = z - (height - 1) * 0.5f;
            SpawnWallColumn($"Left_{z}",  -offsetX, wz);
            SpawnWallColumn($"Right_{z}",  offsetX, wz);
        }
    }

    private void SpawnWallColumn(string id, float wx, float wz)
    {
        for (int y = 0; y < wallHeight; y++)
        {
            var wall = SpawnRandom(wallPrefabs, transform);
            if (wall == null) continue;
            wall.name = $"{WallPrefix}{id}_y{y}";
            wall.transform.localPosition = new Vector3(wx, y, wz);
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale    = Vector3.one;
        }
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────

    private static GameObject SpawnRandom(GameObject[] prefabs, Transform parent)
    {
        var prefab = prefabs[Random.Range(0, prefabs.Length)];
        return prefab == null ? null : Instantiate(prefab, parent);
    }

    private void ClearAll()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (!child.name.StartsWith(TilePrefix) && !child.name.StartsWith(WallPrefix)) continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}
