using TMPro;
using UnityEngine;

public class BattleUI : MonoBehaviour
{
    public TextMeshProUGUI enemyusernameText;

    public void UpdateUI(string username)
    {
        enemyusernameText.text = username;
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
