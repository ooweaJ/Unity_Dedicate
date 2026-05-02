using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// 플레이어의 부쉬 상태를 관리하는 컴포넌트.
///
/// 서버: EnterBush / ExitBush / RevealTemporarily
/// 클라이언트: SyncVar hook → 렌더러 + HP바 On/Off / 반투명 처리
/// </summary>
public class PlayerBushState : NetworkBehaviour
{
    private const float EntryDelay     = 0.5f;
    private const float RevealDuration = 1.0f;
    private const float InBushAlpha    = 0.4f;

    [SyncVar(hook = nameof(OnInBushChanged))]
    public bool inBush;

    [SyncVar(hook = nameof(OnIsRevealedChanged))]
    public bool isRevealed;

    // 현재 속한 부쉬 — 팀 시야 공유 판단에 사용
    [SyncVar(hook = nameof(OnCurrentBushChanged))]
    public NetworkIdentity currentBushIdentity;

    private BattleNetworkPlayer _battlePlayer;
    private PlayerHPBar         _hpBar;
    private Renderer[]          _renderers;
    private Material[][]        _originalMats;
    private Material[][]        _fadeMats;
    private BushZone            _subscribedBush;

    private Coroutine _entryCoroutine;
    private Coroutine _revealCoroutine;

    private void Awake()
    {
        _battlePlayer = GetComponent<BattleNetworkPlayer>();
        _hpBar        = GetComponent<PlayerHPBar>();
    }

    // CharacterSpawner.SpawnVisual 완료 후 호출 (클라이언트)
    public void CacheRenderers(Transform modelRoot)
    {
        _renderers    = modelRoot.GetComponentsInChildren<Renderer>(true);
        _originalMats = new Material[_renderers.Length][];
        _fadeMats     = new Material[_renderers.Length][];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalMats[i] = _renderers[i].sharedMaterials;
            _fadeMats[i]     = CreateFadeMaterialArray(_originalMats[i], InBushAlpha);
        }

        UpdateVisibility();
    }

    // ── 부쉬 진입 / 이탈 (서버) ──────────────────────────────────────────────

    [Server]
    public void EnterBush(BushZone zone)
    {
        if (_entryCoroutine != null) StopCoroutine(_entryCoroutine);
        _entryCoroutine = StartCoroutine(EntryDelayRoutine(zone));
    }

    [Server]
    public void ExitBush()
    {
        if (_entryCoroutine != null)
        {
            StopCoroutine(_entryCoroutine);
            _entryCoroutine = null;
        }

        if (_revealCoroutine != null)
        {
            StopCoroutine(_revealCoroutine);
            _revealCoroutine = null;
            isRevealed       = false;
        }

        inBush              = false;
        currentBushIdentity = null;
    }

    private IEnumerator EntryDelayRoutine(BushZone zone)
    {
        yield return new WaitForSeconds(EntryDelay);
        inBush              = true;
        currentBushIdentity = zone.netIdentity;
        _entryCoroutine     = null;
    }

    // ── 공격 성공 시 노출 (서버) ──────────────────────────────────────────────

    [Server]
    public void RevealTemporarily()
    {
        if (!inBush) return;
        if (_revealCoroutine != null) StopCoroutine(_revealCoroutine);
        _revealCoroutine = StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        isRevealed = true;
        yield return new WaitForSeconds(RevealDuration);
        isRevealed       = false;
        _revealCoroutine = null;
    }

    // ── SyncVar Hooks (클라이언트) ────────────────────────────────────────────

    private void OnInBushChanged(bool _, bool __)       => UpdateVisibility();
    private void OnIsRevealedChanged(bool _, bool __)   => UpdateVisibility();

    private void OnCurrentBushChanged(NetworkIdentity oldBush, NetworkIdentity newBush)
    {
        // 기존 부쉬 이벤트 구독 해제
        if (_subscribedBush != null)
        {
            _subscribedBush.OnVisionMaskChanged -= UpdateVisibility;
            _subscribedBush = null;
        }

        // 새 부쉬 이벤트 구독
        if (newBush != null)
        {
            _subscribedBush = newBush.GetComponent<BushZone>();
            if (_subscribedBush != null)
                _subscribedBush.OnVisionMaskChanged += UpdateVisibility;
        }

        // 로컬 플레이어: 부쉬 메쉬 반투명 전환
        if (isLocalPlayer)
        {
            oldBush?.GetComponent<BushZone>()?.SetLocalPlayerInside(false);
            newBush?.GetComponent<BushZone>()?.SetLocalPlayerInside(true);
        }

        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (_subscribedBush != null)
            _subscribedBush.OnVisionMaskChanged -= UpdateVisibility;

        if (_fadeMats != null)
            foreach (var arr in _fadeMats)
                if (arr != null)
                    foreach (var mat in arr)
                        if (mat != null) Destroy(mat);
    }

    // ── 가시성 계산 ───────────────────────────────────────────────────────────

    private void UpdateVisibility() => SetVisible(IsVisibleToLocalPlayer());

    private bool IsVisibleToLocalPlayer()
    {
        if (isLocalPlayer) return true;
        if (!inBush)       return true;
        if (isRevealed)    return true;

        var local = BattleNetworkPlayer.Local;
        if (local == null) return true;

        if (_battlePlayer != null && _battlePlayer.teamId == local.teamId) return true;

        if (currentBushIdentity != null)
        {
            var bush = currentBushIdentity.GetComponent<BushZone>();
            if (bush != null && bush.HasTeamVision(local.teamId)) return true;
        }

        return false;
    }

    private void SetVisible(bool visible)
    {
        bool fade = visible && inBush;

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.enabled = visible;
                if (visible && _fadeMats != null && _originalMats != null)
                {
                    if (fade) r.materials       = _fadeMats[i];
                    else      r.sharedMaterials  = _originalMats[i];
                }
            }
        }

        _hpBar?.SetBushVisible(visible);
    }

    // ── 머티리얼 유틸리티 ─────────────────────────────────────────────────────

    public static Material[] CreateFadeMaterialArray(Material[] sources, float alpha)
    {
        var result = new Material[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            result[i] = CreateFadeMaterial(sources[i], alpha);
        return result;
    }

    public static Material CreateFadeMaterial(Material src, float alpha)
    {
        var mat = new Material(src);

        // URP Lit
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   0f);
            mat.SetFloat("_ZWrite",  0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        // Standard
        else if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode",   2f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",   0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        if (mat.HasProperty("_BaseColor"))
        {
            var c = mat.GetColor("_BaseColor"); c.a = alpha; mat.SetColor("_BaseColor", c);
        }
        else
        {
            var c = mat.color; c.a = alpha; mat.color = c;
        }

        return mat;
    }
}
