using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드래그 가능한 가상 조이스틱 버튼
///
/// 프리팹 구조:
///   JoystickButton (이 컴포넌트 + Image)
///   ├── Knob         (Image — 드래그 따라 움직이는 점)
///   ├── CooldownFill (Image — Type=Filled, FillMethod=Radial360, 시계방향, 반투명 검정)
///   └── IconImage    (Image — 스킬 아이콘, 쿨다운 중 어둡게)
/// </summary>
public class VirtualJoystickButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("설정")]
    [SerializeField] private float maxDragRadius  = 60f;
    [Tooltip("이 값 이하의 드래그는 발동 안 함 (0~1)")]
    [SerializeField] private float minMagnitude   = 0.15f;

    [Header("UI 참조")]
    [SerializeField] private RectTransform knob;
    [SerializeField] private Image         cooldownFill;  // Radial360 오버레이
    [SerializeField] private Image         iconImage;     // 스킬 아이콘 (쿨다운 중 어둡게)

    [Header("쿨다운 연출")]
    [SerializeField] private float dimAlpha       = 0.4f;   // 쿨다운 중 아이콘 투명도
    [SerializeField] private float readyPulseDuration = 0.25f; // 준비 완료 펄스 시간

    /// <summary>(aimDir XZ 정규화, magnitude 0~1) — 조이스틱 뗄 때 발동</summary>
    public event Action<Vector2, float> OnFire;

    private Vector2   _pointerStart;
    private bool      _isPressed;
    private bool      _wasOnCooldown;
    private Coroutine _pulseRoutine;

    // ─── 터치/마우스 이벤트 ──────────────────────────────────────────────
    public void OnPointerDown(PointerEventData e)
    {
        _isPressed    = true;
        _pointerStart = e.position;
        MoveKnob(Vector2.zero);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_isPressed) return;
        Vector2 delta   = e.position - _pointerStart;
        Vector2 clamped = Vector2.ClampMagnitude(delta, maxDragRadius);
        MoveKnob(clamped);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!_isPressed) return;
        _isPressed = false;

        Vector2 delta     = e.position - _pointerStart;
        float   magnitude = Mathf.Clamp01(delta.magnitude / maxDragRadius);

        if (magnitude >= minMagnitude)
            OnFire?.Invoke(delta.normalized, magnitude);

        MoveKnob(Vector2.zero);
    }

    // ─── 쿨타임 UI ────────────────────────────────────────────────────────
    /// <param name="ratio">0 = 준비완료, 1 = 쿨타임 가득</param>
    public void SetCooldownRatio(float ratio)
    {
        if (cooldownFill != null)
            cooldownFill.fillAmount = ratio;

        bool onCooldown = ratio > 0f;

        if (iconImage != null)
        {
            Color c = iconImage.color;
            c.a = onCooldown ? dimAlpha : 1f;
            iconImage.color = c;
        }

        if (_wasOnCooldown && !onCooldown)
            TriggerReadyPulse();

        _wasOnCooldown = onCooldown;
    }

    private void TriggerReadyPulse()
    {
        if (iconImage == null) return;
        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(ReadyPulse());
    }

    private IEnumerator ReadyPulse()
    {
        float half    = readyPulseDuration * 0.5f;
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / half;
            iconImage.transform.localScale = Vector3.LerpUnclamped(baseScale, baseScale * 1.25f, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / half;
            iconImage.transform.localScale = Vector3.LerpUnclamped(baseScale * 1.25f, baseScale, t);
            yield return null;
        }

        iconImage.transform.localScale = baseScale;
        _pulseRoutine = null;
    }

    private void MoveKnob(Vector2 localDelta)
    {
        if (knob != null)
            knob.anchoredPosition = localDelta;
    }
}
