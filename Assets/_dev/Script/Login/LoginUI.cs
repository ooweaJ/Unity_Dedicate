using Newtonsoft.Json.Linq;
using System;
using TMPro;
using UnityEngine;

public class LoginUI : MonoBehaviour
{
    public event Action<string,string> OnLoginButtonClicked;

    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI resultText;

    public void OnLoginButton()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        OnLoginButtonClicked.Invoke(username, password);
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
