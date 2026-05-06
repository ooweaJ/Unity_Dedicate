using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드래그 가능한 가상 조이스틱 버튼
///
/// 프리팹 구조:
///   JoystickButton (이 컴포넌트 + Image)
///   ├── Knob        (Image — 드래그 따라 움직이는 점)
///   └── CooldownFill (Image — Type=Filled, FillMethod=Radial360, 시계방향)
/// </summary>
public class VirtualJoystickButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("설정")]
    [SerializeField] private float maxDragRadius  = 60f;
    [Tooltip("이 값 이하의 드래그는 발동 안 함 (0~1)")]
    [SerializeField] private float minMagnitude   = 0.15f;

    [Header("UI 참조")]
    [SerializeField] private RectTransform knob;          // 드래그 따라 움직이는 점
    [SerializeField] private Image         cooldownFill;  // fillAmount으로 쿨타임 표시

    /// <summary>(aimDir XZ 정규화, magnitude 0~1) — 조이스틱 뗄 때 발동</summary>
    public event Action<Vector2, float> OnFire;

    private Vector2 _pointerStart;
    private bool    _isPressed;

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
    }

    private void MoveKnob(Vector2 localDelta)
    {
        if (knob != null)
            knob.anchoredPosition = localDelta;
    }
}
