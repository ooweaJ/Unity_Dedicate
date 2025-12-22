using System;
using TMPro;
using UnityEngine;

public class PlayerInfoUI : MonoBehaviour
{
    public event Action OnLogoutButtonClicked;

    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;

    public void LogoutButtonPressed()
    {
        OnLogoutButtonClicked?.Invoke();
    }

    public void UpdateUI(string username, int level, int gold)
    {
        usernameText.text = username;
        levelText.text = "Lv. " + level.ToString();
        goldText.text = gold.ToString();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
