using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// 상태이상 통합 관리 컴포넌트 (서버 권위)
///
/// - Knockback : 클라이언트 Rigidbody에 직접 충격 적용 (RpcApplyKnockback)
/// - Stun      : SyncVar → 이동·공격 잠금
/// - Slow      : SyncVar → 이동 속도 배율 감소
/// - DotDamage : 서버에서 틱마다 TakeDamage 호출
/// - 넉백 중 벽 충돌 → 자동 기절 (OnKnockbackWallHit)
/// </summary>
public class StatusEffectHandler : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnStunnedChanged))]
    private bool  _isStunned;

    [SyncVar(hook = nameof(OnSlowChanged))]
    private float _slowMultiplier = 1f;

    private const float WallStunDuration = 0.5f;

    private PlayerMovement _movement;
    private PlayerStats    _stats;
    private Coroutine      _stunRoutine;
    private Coroutine      _slowRoutine;

    public bool  IsStunned      => _isStunned;
    public float SlowMultiplier => _slowMultiplier;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _stats    = GetComponent<PlayerStats>();
    }

    // ─── 외부 진입점 (서버에서 호출) ────────────────────────────────────────
    [Server]
    public void Apply(StatusEffect effect, Vector3 direction = default, GameObject source = null)
    {
        if (effect.IsNone) return;

        switch (effect.type)
        {
            case StatusEffectType.Knockback:
                if (effect.value > 0f)
                    _movement?.RpcApplyKnockback(direction, effect.value);
                break;

            case StatusEffectType.Stun:
                if (effect.duration > 0f)
                    ApplyStun(effect.duration);
                break;

            case StatusEffectType.Slow:
                if (effect.duration > 0f)
                    ApplySlow(Mathf.Clamp01(effect.value), effect.duration);
                break;

            case StatusEffectType.DotDamage:
                if (effect.interval > 0f && effect.duration > 0f)
                    StartCoroutine(DotCoroutine(effect.value, effect.interval, effect.duration, source));
                break;
        }
    }

    // PlayerMovement가 넉백 중 벽 충돌 감지 시 Command로 호출
    [Server]
    public void OnKnockbackWallHit()
    {
        ApplyStun(WallStunDuration);
    }

    // ─── 내부 구현 ───────────────────────────────────────────────────────────
    [Server]
    private void ApplyStun(float duration)
    {
        if (_stunRoutine != null) StopCoroutine(_stunRoutine);
        _stunRoutine = StartCoroutine(StunCoroutine(duration));
    }

    [Server]
    private void ApplySlow(float multiplier, float duration)
    {
        if (_slowRoutine != null) StopCoroutine(_slowRoutine);
        _slowRoutine = StartCoroutine(SlowCoroutine(multiplier, duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        _isStunned   = true;
        yield return new WaitForSeconds(duration);
        _isStunned   = false;
        _stunRoutine = null;
    }

    private IEnumerator SlowCoroutine(float multiplier, float duration)
    {
        _slowMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        _slowMultiplier = 1f;
        _slowRoutine    = null;
    }

    private IEnumerator DotCoroutine(float dmgPerTick, float interval, float duration, GameObject source)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(interval);
            elapsed += interval;
            if (_stats == null || _stats.IsDead) yield break;
            _stats.TakeDamage(new DamageInfo(dmgPerTick, source));
        }
    }

    // ─── SyncVar 훅 (모든 클라이언트에서 실행) ───────────────────────────────
    private void OnStunnedChanged(bool _, bool stunned)
    {
        GetComponent<PlayerMovement>()?.SetStunned(stunned);
    }

    private void OnSlowChanged(float _, float multiplier)
    {
        GetComponent<PlayerMovement>()?.SetSlowMultiplier(multiplier);
    }
}
