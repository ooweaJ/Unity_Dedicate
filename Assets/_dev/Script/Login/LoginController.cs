using Newtonsoft.Json.Linq;
using UnityEngine;

public class LoginController : MonoBehaviour
{
    [SerializeField] private LoginUI loginUI;
    [SerializeField] private RegisterUI registerUI;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    private void Awake()
    {
        loginUI.OnLoginButtonClicked += HandleLogin;
        loginUI.OnRegisterTabClicked += ShowRegisterPanel;

        registerUI.OnRegisterButtonClicked += HandleRegister;
        registerUI.OnBackToLoginClicked += ShowLoginPanel;
    }

    private void OnDestroy()
    {
        loginUI.OnLoginButtonClicked -= HandleLogin;
        loginUI.OnRegisterTabClicked -= ShowRegisterPanel;

        registerUI.OnRegisterButtonClicked -= HandleRegister;
        registerUI.OnBackToLoginClicked -= ShowLoginPanel;
    }

    // ─── 패널 전환 ────────────────────────────────────────────────────────

    private void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    private void ShowLoginPanel()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    // ─── 로그인 ───────────────────────────────────────────────────────────

    private async void HandleLogin(string username, string password)
    {
        string res = await BackendManager.Login(username, password);
        JObject json = JObject.Parse(res);

        if (json["message"]?.ToString() == "Login success")
        {
            PlayerDataManager.Instance.ApplyUserData(json["userData"]);
            SceneFlowManager.Instance.Load(new LoadRequest
            {
                sceneName = "MainLobbyScene",
                serverAddress = ServerConfig.GameServerIP,
                port = ServerConfig.LobbyPort
            });
        }
        else
        {
            MessagePanel.Show(json["message"]?.ToString() ?? "로그인 실패", UIMessageType.Fail);
        }
    }

    // ─── 회원가입 ─────────────────────────────────────────────────────────

    private async void HandleRegister(string username, string password)
    {
        string res = await BackendManager.Register(username, password);
        JObject json = JObject.Parse(res);

        if (json["success"]?.Value<bool>() == true)
        {
            MessagePanel.Show("회원가입 성공!\n로그인해주세요.", UIMessageType.Success);
            ShowLoginPanel();
        }
        else
        {
            MessagePanel.Show(json["message"]?.ToString() ?? "회원가입 실패", UIMessageType.Fail);
        }
    }
}
