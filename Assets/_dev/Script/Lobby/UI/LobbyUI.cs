using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public event Action OnInventoryButtonClicked;
    public event Action OnStoreButtonClicked;
    public event Action OnStartMatchConfirmed;
    public event Action OnMatchButtonPressed;
    public event Action OnCancelMatch;
    public event Action<int> OnCharacterSelectedInPopup;

    [Header("Confirmation Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private CharacterListManager confirmCharListManager; // 재사용!
    [SerializeField] private TextMeshProUGUI selectedCharName;

    [Header("Matching Timer")]
    [SerializeField] private RectTransform timerRect;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timerShowY = -50f;
    [SerializeField] private float timerHideY = 100f;

    private float _matchStartTime;
    private bool _isMatching;

    private void Update()
    {
        if (_isMatching)
        {
            float elapsed = Time.time - _matchStartTime;
            timerText.text = $"매칭 대기 중... \n{elapsed:F1}s";
        }

        // 12시 방향에서 내려오는 애니메이션 (Lerp)
        float targetY = _isMatching ? timerShowY : timerHideY;
        Vector2 pos = timerRect.anchoredPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * 10f);
        timerRect.anchoredPosition = pos;
    }

    // 캐릭터 리스트 초기화 (ListManager에게 위임)
    public void InitConfirmCharacterList(List<CharacterUIModel> characters, int selectedId)
    {
        if (confirmCharListManager == null) return;

        confirmCharListManager.Init(characters, selectedId, (id) => {
            // 슬롯 클릭 시 호출될 콜백
            OnCharacterSelectedInPopup?.Invoke(id);
            
            // 이름 즉시 갱신
            var selected = characters.Find(c => c.CharacterId == id);
            if (selected != null) selectedCharName.text = selected.Name;
        });

        // 초기 이름 설정
        var initial = characters.Find(c => c.CharacterId == selectedId);
        if (initial != null) selectedCharName.text = initial.Name;
    }

    public void InventoryButtonPressed() => OnInventoryButtonClicked?.Invoke();
    public void StoreButtonPressed() => OnStoreButtonClicked?.Invoke();

    public void OnClickMatchButtonPressed()
    {
        confirmPanel.SetActive(true);
        OnMatchButtonPressed.Invoke();
    }

    public void OnClickConfirmStartMatch()
    {
        confirmPanel.SetActive(false);
        ShowMatchingTimer(true);
        OnStartMatchConfirmed?.Invoke();
    }

    public void OnClickCancelMatch()
    {
        confirmPanel.SetActive(false);
        ShowMatchingTimer(false);
        OnCancelMatch?.Invoke();
    }

    public void ShowMatchingTimer(bool show)
    {
        _isMatching = show;
        if (show) _matchStartTime = Time.time;
    }

    public void Open() => gameObject.SetActive(true);
    public void Close()
    {
        confirmPanel.SetActive(false);
        _isMatching = false;
        gameObject.SetActive(false);
    }
}
