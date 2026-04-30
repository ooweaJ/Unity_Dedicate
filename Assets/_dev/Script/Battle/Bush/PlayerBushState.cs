using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// 플레이어의 부쉬 상태를 관리하는 컴포넌트.
///
/// 서버: EnterBush / ExitBush / RevealTemporarily
/// 클라이언트: SyncVar hook → 렌더러 + HP바 On/Off
/// </summary>
public class PlayerBushState : NetworkBehaviour
{
    private const float EntryDelay     = 0.5f;
    private const float RevealDuration = 1.0f;

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
        _renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
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

        // 새 부쉬 이벤트 구독 — 팀원 진입으로 teamVisionMask 바뀔 때 재계산
        if (newBush != null)
        {
            _subscribedBush = newBush.GetComponent<BushZone>();
            if (_subscribedBush != null)
                _subscribedBush.OnVisionMaskChanged += UpdateVisibility;
        }

        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (_subscribedBush != null)
            _subscribedBush.OnVisionMaskChanged -= UpdateVisibility;
    }

    // ── 가시성 계산 ───────────────────────────────────────────────────────────

    private void UpdateVisibility() => SetVisible(IsVisibleToLocalPlayer());

    private bool IsVisibleToLocalPlayer()
    {
        // 자기 자신은 항상 보임
        if (isLocalPlayer) return true;

        if (!inBush)    return true;
        if (isRevealed) return true;

        var local = BattleNetworkPlayer.Local;
        if (local == null) return true;

        // 같은 팀이면 항상 보임
        if (_battlePlayer != null && _battlePlayer.teamId == local.teamId) return true;

        // 내 팀원이 같은 부쉬에 있으면 보임
        if (currentBushIdentity != null)
        {
            var bush = currentBushIdentity.GetComponent<BushZone>();
            if (bush != null && bush.HasTeamVision(local.teamId)) return true;
        }

        return false;
    }

    private void SetVisible(bool visible)
    {
        if (_renderers != null)
            foreach (var r in _renderers)
                if (r != null) r.enabled = visible;

        _hpBar?.SetBushVisible(visible);
    }
}
