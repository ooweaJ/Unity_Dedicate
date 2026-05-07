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

    private ShaderFadeProfile _fadeProfile;

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
        _fadeProfile  = modelRoot.GetComponentInChildren<CharacterVisualConfig>()?.fadeProfile;

        Debug.Log($"[BushDebug] CacheRenderers | object={gameObject.name} profile={(_fadeProfile != null ? _fadeProfile.name : "null(Auto)")}");

        _renderers    = modelRoot.GetComponentsInChildren<Renderer>(true);

        // 셰이더 프로퍼티 덤프 — 확인 후 삭제
        foreach (var r in _renderers)
        {
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null) continue;
                int count = mat.shader.GetPropertyCount();
                var sb = new System.Text.StringBuilder();
                sb.Append($"[BushDebug] shader={mat.shader.name} properties: ");
                for (int pi = 0; pi < count; pi++)
                    sb.Append($"{mat.shader.GetPropertyName(pi)}({mat.shader.GetPropertyType(pi)}) ");
                Debug.Log(sb.ToString());
            }
        }
        _originalMats = new Material[_renderers.Length][];
        _fadeMats     = new Material[_renderers.Length][];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalMats[i] = _renderers[i].sharedMaterials;
            _fadeMats[i]     = CreateFadeMaterialArray(_originalMats[i], InBushAlpha, _fadeProfile);
        }

        Debug.Log($"[BushDebug] CacheRenderers 완료 | renderer={_renderers.Length}개");

        // 렌더러 캐싱 이후 SyncVar가 이미 바뀐 상태일 수 있으므로 즉시 재계산
        _visibilityDirty = false;
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

    // SyncVar가 같은 프레임에 여러 개 바뀔 수 있으므로 dirty 플래그만 세우고
    // LateUpdate에서 한 번만 계산 → 중간 상태로 깜빡이는 현상 방지
    private bool _visibilityDirty;

    private void OnInBushChanged(bool _, bool __)       => _visibilityDirty = true;
    private void OnIsRevealedChanged(bool _, bool __)   => _visibilityDirty = true;

    private void OnCurrentBushChanged(NetworkIdentity oldBush, NetworkIdentity newBush)
    {
        if (_subscribedBush != null)
        {
            _subscribedBush.OnVisionMaskChanged -= MarkDirty;
            if (isLocalPlayer) _subscribedBush.SetLocalPlayerInside(false);
            _subscribedBush = null;
        }

        if (newBush != null)
        {
            _subscribedBush = newBush.GetComponent<BushZone>();
            if (_subscribedBush != null)
            {
                _subscribedBush.OnVisionMaskChanged += MarkDirty;
                if (isLocalPlayer) _subscribedBush.SetLocalPlayerInside(true);
            }
        }

        _visibilityDirty = true;
    }

    private void MarkDirty() => _visibilityDirty = true;

    private void LateUpdate()
    {
        if (!_visibilityDirty) return;
        _visibilityDirty = false;
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
        Debug.Log($"[BushDebug] SetVisible | visible={visible} inBush={inBush} fade={fade} renderers={_renderers?.Length ?? 0} fadeMats={(_fadeMats != null ? "ready" : "NULL")} originalMats={(_originalMats != null ? "ready" : "NULL")}");

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.enabled = visible;
                if (visible && _fadeMats != null && _originalMats != null)
                {
                    if (fade)
                    {
                        if (_fadeMats[i] != null)
                        {
                            r.materials = _fadeMats[i];
                            if (i == 0) Debug.Log($"[BushDebug] 머티리얼 스왑 → fade | shader={_fadeMats[i][0].shader.name} renderQ={_fadeMats[i][0].renderQueue}");
                        }
                        else Debug.LogWarning($"[BushDebug] _fadeMats[{i}] NULL — 스왑 실패");
                    }
                    else r.sharedMaterials = _originalMats[i];
                }
            }
        }

        _hpBar?.SetBushVisible(visible, fade);
    }

    // ── 머티리얼 유틸리티 ─────────────────────────────────────────────────────

    public static Material[] CreateFadeMaterialArray(Material[] sources, float alpha,
                                                      ShaderFadeProfile profile = null)
    {
        var result = new Material[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            result[i] = CreateFadeMaterial(sources[i], alpha, profile);
        return result;
    }

    public static Material CreateFadeMaterial(Material src, float alpha,
                                               ShaderFadeProfile profile = null)
    {
        var mat = new Material(src);

        if (profile != null && profile.type == ShaderFadeType.PropertyValue)
        {
            bool hasProp = mat.HasProperty(profile.propertyName);
            Debug.Log($"[BushDebug] CreateFadeMaterial | shader={src.shader.name} → PropertyValue | prop={profile.propertyName} exists={hasProp} value={profile.fadeValue}");
            if (hasProp)
                mat.SetFloat(profile.propertyName, profile.fadeValue);

            // UTS Toon 계열: 블렌드 모드를 직접 설정해야 Transparent 패스가 동작
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",   0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // 추가 프로퍼티
            if (profile.additionalProperties != null)
                foreach (var p in profile.additionalProperties)
                    if (mat.HasProperty(p.name))
                        mat.SetFloat(p.name, p.value);
        }
        else
        {
            float a = (profile != null) ? profile.alpha : alpha;

            // URP Lit
            if (mat.HasProperty("_Surface"))
            {
                Debug.Log($"[BushDebug] CreateFadeMaterial | shader={src.shader.name} → URP Lit | alpha={a}");
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend",   0f);
                mat.SetFloat("_ZWrite",  0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            // Standard
            else if (mat.HasProperty("_Mode"))
            {
                Debug.Log($"[BushDebug] CreateFadeMaterial | shader={src.shader.name} → Standard | alpha={a}");
                mat.SetFloat("_Mode",   2f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite",   0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
            }
            else
            {
                Debug.LogWarning($"[BushDebug] CreateFadeMaterial | shader={src.shader.name} → 알 수 없는 셰이더 Fallback | alpha={a}");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite",   0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            if (mat.HasProperty("_BaseColor"))
            {
                var c = mat.GetColor("_BaseColor"); c.a = a; mat.SetColor("_BaseColor", c);
            }
            else
            {
                var c = mat.color; c.a = a; mat.color = c;
            }
        }

        mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

        return mat;
    }
}
