using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 부쉬 구역의 시각적 셀과 BoxCollider를 자동으로 구성하는 컴포넌트.
///
/// 사용법:
///   1. BushZone 오브젝트에 이 컴포넌트를 추가한다.
///   2. CellPrefab: 단일 부쉬 비주얼 프리팹 (Renderer 포함)
///   3. SizeX / SizeZ: 가로·세로 셀 수 (1=단일, 3=3×3 등)
///   4. 인스펙터에서 값 변경 시 자동 갱신, 또는 우클릭 → "Generate Grid"
///
/// 구조 예시 (SizeX=3, SizeZ=3, 부모 Scale=2,2,2):
///   BushZone (scale 2,2,2)
///   ├─ BoxCollider  size=(3,1,3)  → 세계 크기 6×2×6
///   ├─ BushCell_0_0  localPos=(-1, 0,-1)
///   ├─ BushCell_1_0  localPos=( 0, 0,-1)
///   └─ ...
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class BushGridGenerator : MonoBehaviour
{
    private const string CellPrefix = "BushCell_";

    [Header("그리드 크기 (셀 단위)")]
    [SerializeField] [Min(1)] private int sizeX = 1;
    [SerializeField] [Min(1)] private int sizeZ = 1;

    [Header("프리팹")]
    [SerializeField] private GameObject cellPrefab;

    // ── 런타임 초기화 ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Application.isPlaying)
            GenerateGrid();
    }

    // ── 에디터: 인스펙터 값 변경 시 자동 갱신 ────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            // 큐잉 후 플레이 모드 진입 or 빌드 컨텍스트(오브젝트가 에셋 상태)면 중단
            if (Application.isPlaying) return;
            if (!gameObject.scene.IsValid()) return;
            GenerateGrid();
        };
    }
#endif

    // ── 핵심 생성 로직 ────────────────────────────────────────────────────────

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        ClearGeneratedCells();
        ResizeCollider();
        if (cellPrefab != null)
            SpawnCells();
    }

    private void ClearGeneratedCells()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (!child.name.StartsWith(CellPrefix)) continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void ResizeCollider()
    {
        var col = GetComponent<BoxCollider>();

        // X·Z만 갱신, Y(높이·중심)는 사용자가 설정한 값 유지
        col.size   = new Vector3(sizeX, col.size.y, sizeZ);
        col.center = new Vector3(0f, col.center.y, 0f);
    }

    private void SpawnCells()
    {
        // 셀을 0,0 기준으로 중앙 정렬
        // 예) sizeX=3 → x = -1, 0, +1
        // 예) sizeX=2 → x = -0.5, +0.5
        float offsetX = (sizeX - 1) * 0.5f;
        float offsetZ = (sizeZ - 1) * 0.5f;

        for (int ix = 0; ix < sizeX; ix++)
        {
            for (int iz = 0; iz < sizeZ; iz++)
            {
                var cell = Instantiate(cellPrefab, transform);
                cell.name = $"{CellPrefix}{ix}_{iz}";
                cell.transform.localPosition = new Vector3(ix - offsetX, 0f, iz - offsetZ);
                cell.transform.localRotation = Quaternion.identity;
                cell.transform.localScale    = new Vector3(2,2,2);
            }
        }
    }
}
