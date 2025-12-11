using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI resultText;

    public async void OnLoginButton()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        string res = await BackendManager.Login(username, password);
        
        JObject json = JObject.Parse(res);

        if (json["message"].ToString() == "Login success")
        {
            // 유저 정보 저장
            PlayerDataManager.Instance.userId = (int)json["user"]["id"];
            PlayerDataManager.Instance.username = (string)json["user"]["username"];
            PlayerDataManager.Instance.level = (int)json["user"]["level"];
            PlayerDataManager.Instance.gold = (int)json["user"]["gold"];

            resultText.text = "로그인 성공! 로비로 이동 중...";

            // 바로 로비로 이동
            SceneManager.LoadScene("MainLobbyScene");
        }
        else
        {
            resultText.text = "로그인 실패!";
        }
    }
}
