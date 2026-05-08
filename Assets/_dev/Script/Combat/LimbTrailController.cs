using System.Collections;
using UnityEngine;

public enum TrailTrigger { Attack, Skill, Skill2 }

/// <summary>
/// 발/손/무기 등 어느 본에든 붙일 수 있는 범용 트레일 컨트롤러
/// Trigger 필드로 어떤 공격에 반응할지 지정 — PlayerAnimationController가 자동 수집
/// </summary>
public class LimbTrailController : MonoBehaviour
{
    [Tooltip("어떤 공격 타입에 반응할지 — Attack=기본공격, Skill=스킬1, Skill2=스킬2")]
    public TrailTrigger trigger = TrailTrigger.Attack;

    [SerializeField] private TrailRenderer[] trails;

    private Coroutine _activeCoroutine;

    private void Awake()
    {
        SetEmitting(false);
    }

    public void Play(float duration)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(TrailRoutine(duration));
    }

    public void Stop()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
        SetEmitting(false);
    }

    private IEnumerator TrailRoutine(float duration)
    {
        SetEmitting(true);
        yield return new WaitForSeconds(duration);
        SetEmitting(false);
        _activeCoroutine = null;
    }

    private void SetEmitting(bool value)
    {
        if (trails == null) return;
        foreach (var t in trails)
        {
            if (t == null) continue;
            t.emitting = value;
            if (!value) t.Clear();
        }
    }
}
