using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BoxCollider))]
public class WallGenerator : MonoBehaviour
{
    private const string CellPrefix = "WallCell_";

    [Header("벽 크기 (셀 단위)")]
    [SerializeField] [Min(1)] private int sizeX = 3;
    [SerializeField] [Min(1)] private int sizeY = 3;
    [SerializeField] [Min(1)] private int sizeZ = 1;

    [Header("프리팹 (랜덤 선택)")]
    [SerializeField] private GameObject[] wallPrefabs;

    private void Awake()
    {
        if (Application.isPlaying)
            Generate();
    }

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

    [ContextMenu("Generate Wall")]
    public void Generate()
    {
        ClearCells();
        ResizeCollider();
        if (wallPrefabs != null && wallPrefabs.Length > 0)
            SpawnCells();
    }

    private void SpawnCells()
    {
        float offsetX = (sizeX - 1) * 0.5f;
        float offsetY = (sizeY - 1) * 0.5f;
        float offsetZ = (sizeZ - 1) * 0.5f;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    var prefab = wallPrefabs[Random.Range(0, wallPrefabs.Length)];
                    if (prefab == null) continue;

                    var cell = Instantiate(prefab, transform);
                    cell.name = $"{CellPrefix}{x}_{y}_{z}";
                    cell.transform.localPosition = new Vector3(x - offsetX, y - offsetY, z - offsetZ);
                    cell.transform.localRotation = Quaternion.identity;
                    cell.transform.localScale    = Vector3.one;
                }
            }
        }
    }

    private void ResizeCollider()
    {
        var col = GetComponent<BoxCollider>();
        col.size   = new Vector3(sizeX, sizeY, sizeZ);
        col.center = Vector3.zero;
    }

    private void ClearCells()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (!child.name.StartsWith(CellPrefix)) continue;

            if (Application.isPlaying) Destroy(child.gameObject);
            else                       DestroyImmediate(child.gameObject);
        }
    }
}
