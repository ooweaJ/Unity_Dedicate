using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 방향 표시 원 — 모바일에서 항상 표시
/// 기본: 캐릭터 정면 방향 / 조이스틱 드래그 중: 해당 방향
/// </summary>
public class SkillDirectionIndicator : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float distFromOwner = 0.5f;
    [SerializeField] private float circleRadius  = 0.15f;
    [SerializeField] private float yOffset       = 0.3f;
    [SerializeField] private Color circleColor   = Color.white;
    [SerializeField] private float lineWidth     = 0.06f;
    [SerializeField] private int   segments      = 16;

    private LineRenderer _circle;
    private Transform    _owner;
    private Vector3      _overrideDir;
    private bool         _hasOverride;

    private void Awake()
    {
        _circle = CreateCircle();
        _circle.gameObject.SetActive(false);
    }

    /// <summary>모바일 OnStartLocalPlayer에서 호출 — 이후 항상 표시</summary>
    public void Show(Transform owner)
    {
        _owner = owner;
        _circle.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_owner == null) return;

        Vector3 flat = _hasOverride ? _overrideDir : GetDefaultDirection();
        if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;

        Vector3 center = _owner.position + flat * distFromOwner;
        center.y = _owner.position.y + yOffset;
        DrawCircle(center);
    }

    private Vector3 GetDefaultDirection()
    {
        if (IsPCMode())
            return GetMouseDirection();
        return new Vector3(_owner.forward.x, 0f, _owner.forward.z).normalized;
    }

    private Vector3 GetMouseDirection()
    {
        if (Camera.main == null || Mouse.current == null)
            return new Vector3(_owner.forward.x, 0f, _owner.forward.z).normalized;

        var plane = new Plane(Vector3.up, _owner.position);
        var ray   = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (plane.Raycast(ray, out float dist))
        {
            Vector3 dir = ray.GetPoint(dist) - _owner.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                return dir.normalized;
        }

        return new Vector3(_owner.forward.x, 0f, _owner.forward.z).normalized;
    }

    private static bool IsPCMode()
    {
#if UNITY_EDITOR
        return false;
#else
        return !Application.isMobilePlatform;
#endif
    }

    /// <summary>조이스틱 드래그 중 방향 재정의</summary>
    public void SetOverrideDirection(Vector3 worldDir)
    {
        _hasOverride = true;
        Vector3 flat = new Vector3(worldDir.x, 0f, worldDir.z);
        if (flat.sqrMagnitude > 0.001f)
            _overrideDir = flat.normalized;
    }

    /// <summary>조이스틱 뗄 때 — 다시 정면으로</summary>
    public void ClearOverride() => _hasOverride = false;

    private LineRenderer CreateCircle()
    {
        var go = new GameObject("Circle");
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace        = true;
        lr.loop                 = true;
        lr.positionCount        = segments;
        lr.startWidth           = lineWidth;
        lr.endWidth             = lineWidth;
        lr.startColor           = circleColor;
        lr.endColor             = circleColor;
        lr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows       = false;
        lr.generateLightingData = false;
        lr.material             = new Material(Shader.Find("Sprites/Default"));
        return lr;
    }

    private void DrawCircle(Vector3 center)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            _circle.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(angle) * circleRadius,
                center.y,
                center.z + Mathf.Sin(angle) * circleRadius));
        }
    }
}
