using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBar : NetworkBehaviour
{
    [Header("HPBar Canvas 연결")]
    [SerializeField] private Transform hpCanvasTransform;

    [Header("Sliders")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider delaySlider;

    [Header("Delay Bar")]
    [SerializeField] private float delaySpeed = 1.5f;
    [SerializeField] private float delayWait = 0.4f;

    [Header("HP Color")]
    [SerializeField] private Color colorFull = Color.green;
    [SerializeField] private Color colorMid = Color.yellow;
    [SerializeField] private Color colorLow = Color.red;

    [Header("Bush Fade")]
    [SerializeField] [Range(0f, 1f)] private float bushAlpha = 0.4f;

    private Image       hpFillImage;
    private CanvasGroup _canvasGroup;
    private Transform   camTransform;
    private float targetRatio = 1f;
    private float delayTimer = 0f;

    private void Start()
    {
        // 카메라가 씬에 있으므로 Camera.main으로 직접 캐싱
        // GetComponentInChildren<Camera>() 제거
        // → Player 자식에 Camera 없으므로 항상 null이었음
        if (Camera.main != null)
            camTransform = Camera.main.transform;

        if (hpCanvasTransform != null)
            _canvasGroup = hpCanvasTransform.GetComponent<CanvasGroup>()
                        ?? hpCanvasTransform.gameObject.AddComponent<CanvasGroup>();

        if (hpSlider != null && hpSlider.fillRect != null)
            hpFillImage = hpSlider.fillRect.GetComponent<Image>();

        if (hpSlider != null) hpSlider.value = 1f;
        if (delaySlider != null) delaySlider.value = 1f;
        UpdateFillColor(1f);
    }

    private void LateUpdate()
    {
        UpdateHPBarRotation();
        UpdateDelayBar();
    }

    private void UpdateHPBarRotation()
    {
        if (hpCanvasTransform == null) return;

        // camTransform 캐싱 실패 시 재시도 (씬 로딩 타이밍 문제 대비)
        if (camTransform == null)
        {
            Debug.Log("카메라없음");
            if (Camera.main != null)
                camTransform = Camera.main.transform;
            else
                return;
        }

        // World Space RectTransform도 .rotation은 월드 회전
        // LateUpdate에서 덮어쓰면 Player 회전 이후에 적용되므로 회전 고정됨
        hpCanvasTransform.rotation = Quaternion.LookRotation(camTransform.forward);
    }

    private void UpdateDelayBar()
    {
        if (delaySlider == null) return;
        if (delaySlider.value <= targetRatio) return;

        delayTimer -= Time.deltaTime;
        if (delayTimer <= 0f)
        {
            delaySlider.value = Mathf.MoveTowards(
                delaySlider.value,
                targetRatio,
                delaySpeed * Time.deltaTime
            );
        }
    }

    public void UpdateHP(float current, float max)
    {
        float ratio = Mathf.Clamp01(current / max);

        if (hpSlider != null)
            hpSlider.value = ratio;

        if (delaySlider != null)
        {
            if (ratio < delaySlider.value)
                delayTimer = delayWait;
            else
                delaySlider.value = ratio;
        }

        targetRatio = ratio;
        UpdateFillColor(ratio);
    }

    private void UpdateFillColor(float ratio)
    {
        if (hpFillImage == null) return;
        hpFillImage.color = ratio > 0.5f
            ? Color.Lerp(colorMid, colorFull, (ratio - 0.5f) * 2f)
            : Color.Lerp(colorLow, colorMid, ratio * 2f);
    }

    public void SetBushVisible(bool visible, bool fade = false)
    {
        if (hpCanvasTransform != null)
            hpCanvasTransform.gameObject.SetActive(visible);

        if (_canvasGroup != null)
            _canvasGroup.alpha = (visible && fade) ? bushAlpha : 1f;
    }
}