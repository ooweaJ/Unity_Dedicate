using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class LoginUI : MonoBehaviour
{
    public event Action<string, string> OnLoginButtonClicked;
    public event Action OnRegisterTabClicked;

    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    private void Start()
    {
        usernameField.onSubmit.AddListener(_ => OnLoginButton());
        passwordField.onSubmit.AddListener(_ => OnLoginButton());
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame) return;

        var current = EventSystem.current.currentSelectedGameObject;
        if (current == usernameField.gameObject)
            FocusField(passwordField);
        else if (current == passwordField.gameObject)
            FocusField(usernameField);
    }

    private void FocusField(TMP_InputField field)
    {
        field.Select();
        field.ActivateInputField();
    }

    public void OnLoginButton()
    {
        OnLoginButtonClicked?.Invoke(usernameField.text, passwordField.text);
    }

    public void OnRegisterTabButton()
    {
        OnRegisterTabClicked?.Invoke();
    }
}
