using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 이동용 가상 조이스틱
///
/// 프리팹 구조:
///   MovementJoystick (이 컴포넌트 + Image — 배경)
///   └── Knob         (Image — 드래그 따라 움직이는 점)
/// </summary>
public class VirtualMovementJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("설정")]
    [SerializeField] private float maxDragRadius = 60f;

    [Header("UI 참조")]
    [SerializeField] private RectTransform knob;

    /// <summary>매 프레임 방향 벡터 (XZ 이동용). 크기 0~1, 뗄 때 Zero</summary>
    public event Action<Vector2> OnMove;

    /// <summary>외부에서 읽을 수 있는 현재 방향 (magnitude 0~1)</summary>
    public Vector2 Direction { get; private set; }

    private bool _isPressed;

    public void OnPointerDown(PointerEventData e)
    {
        _isPressed = true;
        UpdateKnob(e.position, e.pressPosition);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_isPressed) return;
        UpdateKnob(e.position, e.pressPosition);
    }

    public void OnPointerUp(PointerEventData e)
    {
        _isPressed    = false;
        Direction     = Vector2.zero;
        MoveKnob(Vector2.zero);
        OnMove?.Invoke(Vector2.zero);
    }

    private void Update()
    {
        if (_isPressed)
            OnMove?.Invoke(Direction);
    }

    private void UpdateKnob(Vector2 current, Vector2 origin)
    {
        Vector2 delta   = current - origin;
        Vector2 clamped = Vector2.ClampMagnitude(delta, maxDragRadius);
        Direction       = clamped / maxDragRadius;
        MoveKnob(clamped);
    }

    private void MoveKnob(Vector2 localDelta)
    {
        if (knob != null)
            knob.anchoredPosition = localDelta;
    }
}
