using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RegisterUI : MonoBehaviour
{
    public event Action<string, string> OnRegisterButtonClicked;
    public event Action OnBackToLoginClicked;

    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_InputField confirmPasswordField;

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame) return;

        var current = EventSystem.current.currentSelectedGameObject;
        if (current == usernameField.gameObject)
            FocusField(passwordField);
        else if (current == passwordField.gameObject)
            FocusField(confirmPasswordField);
        else if (current == confirmPasswordField.gameObject)
            FocusField(usernameField);
    }

    private void FocusField(TMP_InputField field)
    {
        field.Select();
        field.ActivateInputField();
    }

    public void OnRegisterButton()
    {
        string username = usernameField.text;
        string password = passwordField.text;
        string confirm  = confirmPasswordField.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessagePanel.Show("아이디와 비밀번호를 입력해주세요.", UIMessageType.Fail);
            return;
        }

        if (password != confirm)
        {
            MessagePanel.Show("비밀번호가 일치하지 않습니다.", UIMessageType.Fail);
            return;
        }

        OnRegisterButtonClicked?.Invoke(username, password);
    }

    public void OnBackToLoginButton()
    {
        OnBackToLoginClicked?.Invoke();
    }
}
