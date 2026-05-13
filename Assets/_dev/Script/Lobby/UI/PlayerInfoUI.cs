using System;
using TMPro;
using UnityEngine;

public class PlayerInfoUI : MonoBehaviour
{
    public event Action OnLogoutButtonClicked;

    [SerializeField] public TextMeshProUGUI usernameText;
    [SerializeField] public TextMeshProUGUI levelText;
    [SerializeField] public TextMeshProUGUI[] goldTexts;
    [SerializeField] public TextMeshProUGUI[] gemTexts;

    [Header("Exp Bar")]
    [SerializeField] private Material expBarMaterial;
    [SerializeField] private string   expProgressProperty = "_Progress";

    private int _progressId;

    private void Awake()
    {
        _progressId = Shader.PropertyToID(expProgressProperty);
    }

    public void LogoutButtonPressed() => OnLogoutButtonClicked?.Invoke();

    public void UpdateUI(string username, int level, int gold, int gem, int exp)
    {
        usernameText.text = username;
        levelText.text    = "Lv. " + level;
        foreach (var t in goldTexts)
            t.text = gold.ToString();
        foreach (var t in gemTexts)
            t.text = gem.ToString();

        int   maxExp   = level * 100;
        float progress = maxExp > 0 ? Mathf.Clamp01((float)exp / maxExp) : 0f;
        expBarMaterial?.SetFloat(_progressId, progress);
    }
}
