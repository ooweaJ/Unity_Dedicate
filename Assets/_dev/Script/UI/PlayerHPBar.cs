using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBar : NetworkBehaviour
{
    [Header("HPBar Canvas 연결")]
    [SerializeField] private GameObject hpCanvas;

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

    [Header("HP바 높이 (월드 Y축 오프셋)")]
    [SerializeField] private float barHeight = 2.5f;

    [Header("Bush Fade")]
    [SerializeField] [Range(0f, 1f)] private float bushAlpha = 0.4f;

    [Header("Team Colors")]
    [SerializeField] private Color allyColorFull = new Color(0.3f, 0.9f, 0.3f);
    [SerializeField] private Color allyColorMid  = new Color(0.9f, 0.9f, 0.3f);
    [SerializeField] private Color allyColorLow  = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color enemyColorFull = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color enemyColorMid  = new Color(0.9f, 0.5f, 0.2f);
    [SerializeField] private Color enemyColorLow  = new Color(0.6f, 0.1f, 0.1f);

    private Image       hpFillImage;
    private CanvasGroup _canvasGroup;
    private float       _currentHP;
    private float       _maxHP;
    private Transform   camTransform;
    private float targetRatio = 1f;
    private float delayTimer = 0f;
    private bool  _teamColorSet = false;

    private string _nickname = "";

    public void SetNickname(string nickname)
    {
        _nickname = nickname;
        if (hpText != null)
            hpText.text = nickname;
    }

    public void ShowStatusText(string text)
    {
        if (hpText != null)
            hpText.text = text;
    }

    public void RestoreNickname()
    {
        if (hpText != null)
            hpText.text = _nickname;
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

        if (hpCanvas != null)
            _canvasGroup = hpCanvas.GetComponent<CanvasGroup>()
                        ?? hpCanvas.AddComponent<CanvasGroup>();

        if (hpSlider != null && hpSlider.fillRect != null)
            hpFillImage = hpSlider.fillRect.GetComponent<Image>();

        if (hpSlider != null) hpSlider.value = 1f;
        if (delaySlider != null) delaySlider.value = 1f;
        UpdateFillColor(1f);
        if (hpText != null) hpText.text = GetComponent<BattleNetworkPlayer>()?.nickname ?? "";
    }

    private void LateUpdate()
    {
        // 로컬 플레이어 확정 후 팀 색상 1회 적용
        if (!_teamColorSet && BattleNetworkPlayer.Local != null)
        {
            var self  = GetComponent<BattleNetworkPlayer>();
            bool ally = self == null || self.teamId == BattleNetworkPlayer.Local.teamId;
            SetTeamColors(ally);
            _teamColorSet = true;
        }

        UpdateHPBarRotation();
        UpdateDelayBar();
    }

    public void SetTeamColors(bool isAlly)
    {
        colorFull = isAlly ? allyColorFull : enemyColorFull;
        colorMid  = isAlly ? allyColorMid  : enemyColorMid;
        colorLow  = isAlly ? allyColorLow  : enemyColorLow;
        UpdateFillColor(_maxHP > 0f ? _currentHP / _maxHP : 1f);
    }

    private void UpdateHPBarRotation()
    {
        if (hpCanvas == null) return;

        if (camTransform == null)
        {
            if (Camera.main != null)
                camTransform = Camera.main.transform;
            else
                return;
        }

        // 월드 Y만 올려서 고정 — XZ 오프셋 없음 (진영·카메라 각도 무관)
        hpCanvas.transform.position = transform.position + Vector3.up * barHeight;
        // 카메라 정면을 향해 회전 (빌보드)
        hpCanvas.transform.rotation = Quaternion.LookRotation(camTransform.forward);
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

    public void Hide()
    {
        hpCanvas?.SetActive(false);
    }

    public void SetBushVisible(bool visible, bool fade = false)
    {
        hpCanvas?.SetActive(visible);

        if (_canvasGroup != null)
            _canvasGroup.alpha = (visible && fade) ? bushAlpha : 1f;
    }
}