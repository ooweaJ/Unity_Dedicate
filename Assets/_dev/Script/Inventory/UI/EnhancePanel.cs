using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancePanel : MonoBehaviour
{
    [SerializeField] private Image    itemIcon;
    [SerializeField] private Image    enhanceAnimImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text currentEnhanceText;  // "+N → +N+1"
    [SerializeField] private TMP_Text successRateText;     // "X%"
    [SerializeField] private TMP_Text goldCostText;        // "N G"
    [SerializeField] private Button   enhanceButton;
    [SerializeField] private Button   closeButton;

    [SerializeField] private float animColorInterval = 0.08f;
    [SerializeField] private float minAnimDuration   = 2f;
    [SerializeField] private float rotateSpeed       = 360f;

    public event Action<int> OnEnhanceRequested;

    private int       _equipInstanceId;
    private Coroutine _animCoroutine;
    private float     _animStartTime;

    private static readonly Color ResultSuccess = new Color(0.2f, 0.5f, 1f);
    private static readonly Color ResultFail    = new Color(1f,   0.2f, 0.2f);

    private void Awake()
    {
        enhanceButton?.onClick.AddListener(OnEnhanceClicked);
        closeButton?.onClick.AddListener(Hide);
    }

    private void OnEnhanceClicked()
    {
        if (enhanceButton != null) enhanceButton.interactable = false;
        _animStartTime = Time.time;
        StopAnim();
        _animCoroutine = StartCoroutine(AnimateColors());
        OnEnhanceRequested?.Invoke(_equipInstanceId);
    }

    public void Open(int equipInstanceId, string itemName, Sprite icon = null)
    {
        _equipInstanceId = equipInstanceId;
        gameObject.SetActive(true);

        if (itemNameText != null) itemNameText.text = itemName;
        if (itemIcon != null)
        {
            itemIcon.sprite  = icon;
            itemIcon.enabled = icon != null;
        }
        if (enhanceAnimImage != null)
        {
            enhanceAnimImage.color                   = Color.white;
            enhanceAnimImage.transform.localRotation = Quaternion.identity;
        }

        StopAnim();
        SetLoadingState();
    }

    public void SetPreviewData(int currentEnhance, float successRate, int goldCost)
    {
        if (currentEnhanceText != null) currentEnhanceText.text    = $"+{currentEnhance}  →  +{currentEnhance + 1}";
        if (successRateText    != null) successRateText.text        = $"{(int)(successRate * 100)}%";
        if (goldCostText       != null) goldCostText.text           = $"{goldCost:N0} G";
        if (enhanceButton      != null) enhanceButton.interactable  = true;
    }

    public void SetMaxEnhance()
    {
        if (currentEnhanceText != null) currentEnhanceText.text    = "+8 (최대)";
        if (successRateText    != null) successRateText.text        = "-";
        if (goldCostText       != null) goldCostText.text           = "-";
        if (enhanceButton      != null) enhanceButton.interactable  = false;
    }

    public void ShowResult(bool success, int enhance)
    {
        StartCoroutine(ShowResultAfterMinDuration(success, enhance));
    }

    public void ShowError(string message)
    {
        StopAnim();
        if (enhanceAnimImage != null)
        {
            enhanceAnimImage.color                   = Color.white;
            enhanceAnimImage.transform.localRotation = Quaternion.identity;
        }
        if (enhanceButton != null) enhanceButton.interactable = false;
    }

    public void Hide()
    {
        StopAnim();
        gameObject.SetActive(false);
    }

    // ── 코루틴 ────────────────────────────────────────────────────────

    private IEnumerator AnimateColors()
    {
        while (true)
        {
            if (enhanceAnimImage != null)
            {
                enhanceAnimImage.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
                enhanceAnimImage.transform.Rotate(0f, 0f, rotateSpeed * animColorInterval);
            }
            yield return new WaitForSeconds(animColorInterval);
        }
    }

    private IEnumerator ShowResultAfterMinDuration(bool success, int enhance)
    {
        float elapsed = Time.time - _animStartTime;
        if (elapsed < minAnimDuration)
            yield return new WaitForSeconds(minAnimDuration - elapsed);

        StopAnim();
        if (enhanceAnimImage != null)
        {
            enhanceAnimImage.color                   = success ? ResultSuccess : ResultFail;
            enhanceAnimImage.transform.localRotation = Quaternion.identity;
        }
        if (enhanceButton != null) enhanceButton.interactable = false;
    }

    private void StopAnim()
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
    }

    private void SetLoadingState()
    {
        if (currentEnhanceText != null) currentEnhanceText.text   = "...";
        if (successRateText    != null) successRateText.text       = "...";
        if (goldCostText       != null) goldCostText.text          = "...";
        if (enhanceButton      != null) enhanceButton.interactable = false;
    }
}
