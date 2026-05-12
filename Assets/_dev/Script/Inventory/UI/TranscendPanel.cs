using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TranscendPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text currentStageText;
    [SerializeField] private TMP_Text shardInfoText;
    [SerializeField] private TMP_Text shardsToUseText;
    [SerializeField] private TMP_Text probabilityText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button   btnMinus;
    [SerializeField] private Button   btnPlus;
    [SerializeField] private Button   transcendButton;
    [SerializeField] private Button   closeButton;

    public event Action<int, int> OnTranscendRequested; // (characterId, shardsToUse)

    private int _characterId;
    private int _shardsRequired;
    private int _maxShardsToUse;
    private int _shardsToUse;

    private void Awake()
    {
        btnMinus?.onClick.AddListener(() => SetShardsToUse(_shardsToUse - 1));
        btnPlus?.onClick.AddListener(() => SetShardsToUse(_shardsToUse + 1));
        transcendButton?.onClick.AddListener(OnTranscendClicked);
        closeButton?.onClick.AddListener(Hide);
    }

    public void Open(int characterId, string characterName, int currentStage, int currentShards)
    {
        _characterId     = characterId;
        _shardsRequired  = currentStage + 1;
        _maxShardsToUse  = Mathf.Min(currentShards, _shardsRequired);

        if (characterNameText != null) characterNameText.text = characterName;
        if (currentStageText  != null) currentStageText.text  = $"{currentStage}단계";
        if (shardInfoText     != null) shardInfoText.text     = $"보유 조각: {currentShards} / 필요: {_shardsRequired}";
        if (statusText        != null) statusText.text        = "";

        SetButtonsInteractable(true);

        if (_maxShardsToUse <= 0)
        {
            if (statusText        != null) statusText.text             = $"조각이 부족합니다. (필요: {_shardsRequired}개)";
            if (shardsToUseText   != null) shardsToUseText.text        = "0개";
            if (probabilityText   != null) probabilityText.text        = "0%";
            if (transcendButton   != null) transcendButton.interactable = false;
            if (btnMinus          != null) btnMinus.interactable        = false;
            if (btnPlus           != null) btnPlus.interactable         = false;
        }
        else
        {
            SetShardsToUse(1);
        }

        gameObject.SetActive(true);
    }

    public void ShowResult(bool transcendSuccess, int transcendStage)
    {
        if (statusText != null)
            statusText.text = transcendSuccess
                ? $"초월 성공! ({transcendStage}단계 달성)"
                : "초월 실패... 조각은 소모됩니다.";
        SetButtonsInteractable(false);
    }

    public void ShowError(string message)
    {
        if (statusText != null) statusText.text = message;
        SetButtonsInteractable(false);
    }

    public void Hide() => gameObject.SetActive(false);

    // ── private ──────────────────────────────────────────────────────

    private void SetShardsToUse(int value)
    {
        _shardsToUse = Mathf.Clamp(value, 1, _maxShardsToUse);

        float probability = (float)_shardsToUse / _shardsRequired * 100f;
        if (shardsToUseText != null) shardsToUseText.text = $"{_shardsToUse}개";
        if (probabilityText != null) probabilityText.text = $"{(int)probability}%";

        if (btnMinus != null) btnMinus.interactable = _shardsToUse > 1;
        if (btnPlus  != null) btnPlus.interactable  = _shardsToUse < _maxShardsToUse;
    }

    private void OnTranscendClicked()
    {
        SetButtonsInteractable(false);
        if (statusText != null) statusText.text = "초월 시도 중...";
        OnTranscendRequested?.Invoke(_characterId, _shardsToUse);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (transcendButton != null) transcendButton.interactable = value;
        if (btnMinus        != null) btnMinus.interactable        = value && _shardsToUse > 1;
        if (btnPlus         != null) btnPlus.interactable         = value && _shardsToUse < _maxShardsToUse;
    }
}
