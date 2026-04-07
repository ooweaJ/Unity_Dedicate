using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    [SerializeField] private LoginUI loginUI;

    private void Awake()
    {
        loginUI.OnLoginButtonClicked += HandleLogin;
    }

    private void OnDestroy()
    {
        loginUI.OnLoginButtonClicked -= HandleLogin;
    }
    public async void HandleLogin(string username, string password)
    {
        string res = await BackendManager.Login(username, password);

        JObject json = JObject.Parse(res);

        if (json["message"] != null && json["message"].ToString() == "Login success")
        {
            // 1. 로그인 응답에 포함된 유저+캐릭터+아이템 정보를 통째로 데이터 매니저에 적용
            // ApplyUserData 내부에서 캐릭터 리스트까지 한 번에 처리하도록 만들면 베스트!
            PlayerDataManager.Instance.ApplyUserData(json["userData"]);

            // 2. 이제 추가 서버 통신 없이 바로 씬 전환으로 넘어갑니다.
            InitAfterLogin();
        }
        else
        {
            Debug.Log($"로그인 실패: {json["message"]}");
        }
    }

    // 플레이어 데이터를 넣은 후 로비로 이동
    public void InitAfterLogin() 
    {
        SceneFlowManager.Instance.Load(new LoadRequest
        {
            sceneName = "MainLobbyScene",
            serverAddress = "127.0.0.1",
            port = 7777
        });
    }
}
