using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TranscendPanel : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text stageText;        // "N단계  →  N+1단계"
    [SerializeField] private TMP_Text shardCountText;   // "보유 조각 : N"
    [SerializeField] private TMP_Text shardsRatioText;  // "( 투입수 / 최대수 )"  — −/+ 버튼 사이
    [SerializeField] private TMP_Text probabilityText;  // "확률 : X%"

    [Header("Buttons")]
    [SerializeField] private Button btnMinus;
    [SerializeField] private Button btnPlus;
    [SerializeField] private Button transcendButton;

    public event Action<int, int> OnTranscendRequested; // (characterId, shardsToUse)

    private int _characterId;
    private int _shardsRequired;   // 이번 초월에 필요한 총 조각 수 (= 다음 단계 숫자)
    private int _maxShardsToUse;   // 실제 투입 가능 최대 = min(보유조각, _shardsRequired)
    private int _shardsToUse;      // 현재 선택된 투입 수

    private void Awake()
    {
        btnMinus?.onClick.AddListener(() => SetShardsToUse(_shardsToUse - 1));
        btnPlus?.onClick.AddListener(() => SetShardsToUse(_shardsToUse + 1));
        transcendButton?.onClick.AddListener(OnTranscendClicked);
    }

    // 캐릭터 선택 시 호출 — 탭에 항상 표시되므로 Open/Hide 없음
    public void Init(int characterId, int currentStage, int currentShards)
    {
        _characterId    = characterId;
        _shardsRequired = currentStage + 1;
        _maxShardsToUse = Mathf.Min(currentShards, _shardsRequired);

        if (stageText      != null) stageText.text      = $"{currentStage}단계  →  {currentStage + 1}단계";
        if (shardCountText != null) shardCountText.text = $"보유 조각 : {currentShards}";

        if (_maxShardsToUse > 0)
        {
            SetShardsToUse(1);
            if (transcendButton != null) transcendButton.interactable = true;
        }
        else
        {
            if (shardsRatioText != null) shardsRatioText.text        = $"( 0 / {_shardsRequired} )";
            if (probabilityText != null) probabilityText.text        = "확률 : 0%";
            if (btnMinus        != null) btnMinus.interactable       = false;
            if (btnPlus         != null) btnPlus.interactable        = false;
            if (transcendButton != null) transcendButton.interactable = false;
        }
    }

    public void ShowResult(bool transcendSuccess, int transcendStage)
    {
        SetInteractable(false);
    }

    public void ShowError(string message)
    {
        SetInteractable(false);
    }

    // ── private ──────────────────────────────────────────────────────

    private void SetShardsToUse(int value)
    {
        _shardsToUse = Mathf.Clamp(value, 1, _maxShardsToUse);

        float probability = (float)_shardsToUse / _shardsRequired * 100f;

        if (shardsRatioText != null) shardsRatioText.text  = $"( {_shardsToUse} / {_shardsRequired} )";
        if (probabilityText != null) probabilityText.text  = $"확률 : {(int)probability}%";
        if (btnMinus        != null) btnMinus.interactable = _shardsToUse > 1;
        if (btnPlus         != null) btnPlus.interactable  = _shardsToUse < _maxShardsToUse;
    }

    private void OnTranscendClicked()
    {
        SetInteractable(false);
        OnTranscendRequested?.Invoke(_characterId, _shardsToUse);
    }

    private void SetInteractable(bool value)
    {
        if (transcendButton != null) transcendButton.interactable = value;
        if (btnMinus        != null) btnMinus.interactable        = value && _shardsToUse > 1;
        if (btnPlus         != null) btnPlus.interactable         = value && _shardsToUse < _maxShardsToUse;
    }
}
