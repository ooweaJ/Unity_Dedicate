using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancePanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text   itemNameText;
    [SerializeField] private TMP_Text   currentEnhanceText;
    [SerializeField] private TMP_Text   successRateText;
    [SerializeField] private TMP_Text   goldCostText;
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private Button     enhanceButton;
    [SerializeField] private Button     closeButton;

    public event Action<int> OnEnhanceRequested;

    private int _equipInstanceId;

    private void Awake()
    {
        enhanceButton?.onClick.AddListener(() => OnEnhanceRequested?.Invoke(_equipInstanceId));
        closeButton?.onClick.AddListener(Hide);
    }

    public void Open(int equipInstanceId, string itemName)
    {
        _equipInstanceId = equipInstanceId;
        panel.SetActive(true);
        if (itemNameText != null) itemNameText.text = itemName;
        SetLoadingState();
    }

    public void SetPreviewData(int currentEnhance, float successRate, int goldCost)
    {
        if (currentEnhanceText != null) currentEnhanceText.text = $"+{currentEnhance}  →  +{currentEnhance + 1}";
        if (successRateText != null)    successRateText.text    = $"{(int)(successRate * 100)}%";
        if (goldCostText != null)       goldCostText.text       = $"{goldCost:N0} G";
        if (statusText != null)         statusText.text         = "";
        if (enhanceButton != null)      enhanceButton.interactable = true;
    }

    public void SetMaxEnhance()
    {
        if (currentEnhanceText != null) currentEnhanceText.text = "+8 (최대)";
        if (successRateText != null)    successRateText.text    = "-";
        if (goldCostText != null)       goldCostText.text       = "-";
        if (statusText != null)         statusText.text         = "최대 강화 상태입니다.";
        if (enhanceButton != null)      enhanceButton.interactable = false;
    }

    public void ShowResult(bool success, int enhance)
    {
        if (statusText != null)
            statusText.text = success ? $"강화 성공!  (+{enhance})" : "강화 실패...";
        if (enhanceButton != null) enhanceButton.interactable = false;
    }

    public void ShowError(string message)
    {
        if (statusText != null)    statusText.text            = message;
        if (enhanceButton != null) enhanceButton.interactable = false;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void SetLoadingState()
    {
        if (currentEnhanceText != null) currentEnhanceText.text    = "...";
        if (successRateText != null)    successRateText.text        = "...";
        if (goldCostText != null)       goldCostText.text           = "...";
        if (statusText != null)         statusText.text             = "";
        if (enhanceButton != null)      enhanceButton.interactable  = false;
    }
}
