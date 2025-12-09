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

        string res = await Backend.Login(username, password);
        
        JObject json = JObject.Parse(res);

        if (json["message"].ToString() == "Login success")
        {
            // 유저 정보 저장
            PlayerDataManager.Instance.userId = (int)json["user"]["id"];
            PlayerDataManager.Instance.username = (string)json["user"]["username"];

            resultText.text = "로그인 성공! 캐릭터 불러오는 중...";

            // 캐릭터 불러오기
            await LoadCharactersAndMove();
        }
        else
        {
            resultText.text = "로그인 실패!";
        }
    }

    private async Task LoadCharactersAndMove()
    {
        string res = await Backend.GetCharacters(PlayerDataManager.Instance.userId);
        JArray arr = JArray.Parse(res);

        PlayerDataManager.Instance.characters.Clear();

        foreach (var c in arr)
        {
            PlayerDataManager.Instance.characters.Add(new Character
            {
                id = (int)c["id"],
                name = (string)c["name"],
                level = (int)c["level"],
                hp = (int)c["hp"],
                atk = (int)c["atk"]
            });
        }

        // 캐릭터 선택 씬으로 이동
        SceneManager.LoadScene("CharacterSelect");
    }
}
