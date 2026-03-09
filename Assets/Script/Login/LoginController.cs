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


        if (json["message"].ToString() == "Login success")
        {
            PlayerDataManager.Instance.ApplyUserData(json["user"]);

            await InitAfterLogin(PlayerDataManager.Instance.GetUserId());
        }
        else
        {

        }
    }

    public async Task InitAfterLogin(int userId)
    {
        await LoadUserCharacters(userId);
        CustomNetworkManager.Instance.StartClient();
    }

    async Task LoadUserCharacters(int userId)
    {
        string res = await BackendManager.GetUserCharacters(userId);
        JObject json = JObject.Parse(res);

        JArray arr = (JArray)json["characters"];

        foreach (var j in arr)
        {
            PlayerCharacterData pc = new PlayerCharacterData
            {
                characterId = (int)j["characterId"],
                level = (int)j["level"],
                exp = (int)j["exp"],
                enhance = (int)j["enhance"]
            };

            PlayerDataManager.Instance.CharacterAddOrUpdate(pc);
        }
    }
}
