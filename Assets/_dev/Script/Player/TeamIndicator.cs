using Mirror;
using UnityEngine;
using UnityEngine.Rendering;

public class TeamIndicator : NetworkBehaviour
{
    [SerializeField] private float yOffset    = 0.05f;
    [SerializeField] private float radius     = 0.75f;  // 바깥 반지름
    [SerializeField] private float innerRatio = 0.55f;  // 빈 중심 비율 (클수록 구멍 커짐)
    [SerializeField] private int   segments   = 48;

    // 파스텔 톤
    static readonly Color ColorSelf  = new Color(0.45f, 1.0f, 0.55f, 1f);
    static readonly Color ColorAlly  = new Color(0.45f, 0.72f, 1.0f,  1f);
    static readonly Color ColorEnemy = new Color(1.0f,  0.45f, 0.45f, 1f);

    private GameObject          _root;
    private MeshRenderer        _mr;
    private Mesh                _mesh;
    private PlayerBushState     _bushState;
    private BattleNetworkPlayer _bp;
    private PlayerStats         _stats;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _bushState = GetComponent<PlayerBushState>();
        _bp        = GetComponent<BattleNetworkPlayer>();
        _stats     = GetComponent<PlayerStats>();

        BuildDisc();
        BattleNetworkPlayer.OnPlayerJoined += OnAnyPlayerJoined;
        RefreshColor();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        BattleNetworkPlayer.OnPlayerJoined -= OnAnyPlayerJoined;
        if (_root != null) Destroy(_root);
    }

    public void Hide()
    {
        if (_root != null) _root.SetActive(false);
    }

    private void OnAnyPlayerJoined(BattleNetworkPlayer _) => RefreshColor();

    private void LateUpdate()
    {
        if (_root == null) return;
        if (_stats != null && _stats.IsDead) { _root.SetActive(false); return; }
        _root.SetActive(IsVisibleToLocal());
    }

    private bool IsVisibleToLocal()
    {
        if (_bushState == null)    return true;
        if (isLocalPlayer)         return true;
        if (!_bushState.inBush)    return true;
        if (_bushState.isRevealed) return true;

        var local = BattleNetworkPlayer.Local;
        if (local == null) return true;
        if (_bp != null && _bp.teamId == local.teamId) return true;

        if (_bushState.currentBushIdentity != null)
        {
            var bush = _bushState.currentBushIdentity.GetComponent<BushZone>();
            if (bush != null && bush.HasTeamVision(local.teamId)) return true;
        }

        return false;
    }

    private void BuildDisc()
    {
        _root = new GameObject("TeamIndicator");
        _root.transform.SetParent(transform, false);
        _root.transform.localPosition = new Vector3(0f, yOffset, 0f);

        var mf = _root.AddComponent<MeshFilter>();
        _mr    = _root.AddComponent<MeshRenderer>();

        _mr.shadowCastingMode = ShadowCastingMode.Off;
        _mr.receiveShadows    = false;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        _mr.material = new Material(shader);

        _mesh = CreateRingMesh(Color.white);
        mf.mesh = _mesh;
    }

    // 안쪽 링(alpha=0) + 바깥 링(alpha=1) 도넛 메시
    private Mesh CreateRingMesh(Color baseColor)
    {
        var mesh   = new Mesh();
        int count  = segments;
        float innerR = radius * innerRatio;

        // 버텍스: [0..count-1] = 안쪽 링, [count..2count-1] = 바깥 링
        var verts  = new Vector3[count * 2];
        var colors = new Color[count * 2];
        var tris   = new int[count * 6];

        float step = 2f * Mathf.PI / count;
        for (int i = 0; i < count; i++)
        {
            float a   = i * step;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);

            // 안쪽: 완전 투명
            verts[i]  = new Vector3(cos * innerR, 0f, sin * innerR);
            colors[i] = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            // 바깥: 완전 불투명 (진한 테두리)
            verts[i + count]  = new Vector3(cos * radius, 0f, sin * radius);
            colors[i + count] = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

            // 쿼드 2삼각형
            int next = (i + 1) % count;
            int b    = i * 6;
            tris[b]     = i;
            tris[b + 1] = i + count;
            tris[b + 2] = next + count;
            tris[b + 3] = i;
            tris[b + 4] = next + count;
            tris[b + 5] = next;
        }

        mesh.vertices  = verts;
        mesh.colors    = colors;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void RefreshColor()
    {
        if (_mesh == null) return;

        var local = BattleNetworkPlayer.Local;

        Color teamColor;
        if (isLocalPlayer)
            teamColor = ColorSelf;
        else if (local != null && _bp != null && _bp.teamId == local.teamId)
            teamColor = ColorAlly;
        else
            teamColor = ColorEnemy;

        var colors = _mesh.colors;
        int count  = segments;

        for (int i = 0; i < count; i++)
        {
            // 안쪽: 팀 컬러지만 alpha=0
            colors[i] = new Color(teamColor.r, teamColor.g, teamColor.b, 0f);
            // 바깥: 팀 컬러 alpha=1 (진한 테두리)
            colors[i + count] = teamColor;
        }

        _mesh.colors = colors;
    }
}
