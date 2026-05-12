using Mirror;
using TMPro;
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

    [Header("HP Text")]
    [SerializeField] private TMP_Text hpText;

    [Header("World Space Offset (플레이어 회전과 무관한 고정 오프셋)")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, -1.5f);

    [Header("Bush Fade")]
    [SerializeField] [Range(0f, 1f)] private float bushAlpha = 0.4f;

    private Image       hpFillImage;
    private CanvasGroup _canvasGroup;
    private float       _currentHP;
    private float       _maxHP;
    private Transform   camTransform;
    private float targetRatio = 1f;
    private float delayTimer = 0f;

    public void SetNickname(string nickname)
    {
        if (hpText != null)
            hpText.text = nickname;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        var bnp = GetComponent<BattleNetworkPlayer>();
        if (bnp != null && !string.IsNullOrEmpty(bnp.nickname))
            SetNickname(bnp.nickname);
    }

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
        if (hpText != null) hpText.text = GetComponent<BattleNetworkPlayer>()?.nickname ?? "";
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

        // 위치: 월드 오프셋으로 고정 — 로컬 오프셋은 플레이어 회전 시 같이 돌아가므로 사용 안 함
        hpCanvasTransform.position = transform.position + worldOffset;
        // 회전: 카메라 방향을 향하도록 고정
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
        _currentHP = current;
        _maxHP     = max;

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

        // hpText는 닉네임 고정 표시 — HP 숫자로 덮어쓰지 않음
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