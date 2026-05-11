using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 수류탄 조준 인디케이터.
/// - 착탄점: 작은 노란 원 (LineRenderer)
/// - 폭발 범위: 채워진 빨간 disc (MeshRenderer) + 테두리 (LineRenderer)
/// </summary>
public class GrenadeAimIndicator : MonoBehaviour
{
    [Header("색상")]
    [SerializeField] private Color landingColor = new Color(1f, 0.95f, 0f, 1f);
    [SerializeField] private Color fillColor    = new Color(1f, 0.2f,  0.2f, 0.4f);
    [SerializeField] private Color borderColor  = new Color(1f, 0.2f,  0.2f, 1f);

    [Header("라인 굵기")]
    [SerializeField] private float landingWidth = 0.12f;
    [SerializeField] private float borderWidth  = 0.08f;

    [Header("원 정밀도")]
    [SerializeField] private int segments = 48;

    private LineRenderer _landingCircle;
    private Transform    _fillDisc;
    private LineRenderer _borderCircle;

    private float _maxDistance;
    private float _explosionRadius;
    private bool  _active;

    public bool    IsActive  => _active;
    public Vector3 TargetPos { get; private set; }

    // ─── 초기화 ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _landingCircle = CreateLineCircle("LandingCircle", landingWidth, landingColor);
        _fillDisc      = CreateDiscMesh("RadiusFill", fillColor);
        _borderCircle  = CreateLineCircle("RadiusBorder", borderWidth, borderColor);
        SetVisible(false);
    }

    // ─── 공개 API ─────────────────────────────────────────────────────────

    public void BeginAim(float maxDistance, float explosionRadius)
    {
        _maxDistance     = maxDistance;
        _explosionRadius = explosionRadius;
        _active          = true;
        SetVisible(true);
    }

    public void EndAim()
    {
        _active = false;
        SetVisible(false);
    }

    // ─── 매 프레임 갱신 ───────────────────────────────────────────────────

    private void Update()
    {
        if (!_active || Camera.main == null || Mouse.current == null) return;

        var plane = new Plane(Vector3.up, transform.position);
        var ray   = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 raw   = ray.GetPoint(enter);
            Vector3 delta = new Vector3(raw.x - transform.position.x, 0f, raw.z - transform.position.z);
            if (delta.magnitude > _maxDistance)
                raw = transform.position + delta.normalized * _maxDistance;
            TargetPos = new Vector3(raw.x, transform.position.y, raw.z);
        }

        float y = TargetPos.y + 0.05f;

        UpdateLineCircle(_landingCircle, TargetPos, 0.5f, y);

        // disc는 fill 위에 border가 올라오도록 fill을 살짝 낮게
        _fillDisc.position   = new Vector3(TargetPos.x, y - 0.01f, TargetPos.z);
        _fillDisc.localScale = Vector3.one * (_explosionRadius * 2f);
        UpdateLineCircle(_borderCircle, TargetPos, _explosionRadius, y);
    }

    // ─── LineRenderer 원 ──────────────────────────────────────────────────

    private LineRenderer CreateLineCircle(string goName, float width, Color color)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace        = true;
        lr.loop                 = true;
        lr.positionCount        = segments;
        lr.startWidth           = width;
        lr.endWidth             = width;
        lr.startColor           = color;
        lr.endColor             = color;
        lr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows       = false;
        lr.generateLightingData = false;
        lr.material             = new Material(Shader.Find("Sprites/Default"));

        return lr;
    }

    private void UpdateLineCircle(LineRenderer lr, Vector3 center, float radius, float y)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            lr.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(angle) * radius, y,
                center.z + Mathf.Sin(angle) * radius));
        }
    }

    // ─── 채워진 disc 메시 ─────────────────────────────────────────────────

    private Transform CreateDiscMesh(string goName, Color color)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        mf.mesh = BuildDiscMesh(segments);

        var mat = new Material(Shader.Find("Sprites/Default")) { color = color };
        mr.material          = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        return go.transform;
    }

    // XZ 평면 원형 메시 (반지름 0.5, scale로 크기 조절)
    private static Mesh BuildDiscMesh(int segs)
    {
        var mesh  = new Mesh();
        var verts = new Vector3[segs + 1];
        var tris  = new int[segs * 3];

        verts[0] = Vector3.zero;
        for (int i = 0; i < segs; i++)
        {
            float angle = i * Mathf.PI * 2f / segs;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);

            int next = (i < segs - 1) ? i + 2 : 1;
            tris[i * 3]     = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = next;
        }

        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    // ─── 표시 토글 ────────────────────────────────────────────────────────

    private void SetVisible(bool show)
    {
        if (_landingCircle) _landingCircle.gameObject.SetActive(show);
        if (_fillDisc)      _fillDisc.gameObject.SetActive(show);
        if (_borderCircle)  _borderCircle.gameObject.SetActive(show);
    }
}
