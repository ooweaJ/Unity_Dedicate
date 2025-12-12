using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

public class LobbyManager : MonoBehaviour
{
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;

    // 가챠 패널(UI 패널)을 씬에서 비활성화 상태로 두고 여기에 연결하면 됨
    public GameObject gachaPanel; // Optional: 가챠 UI 패널 할당
    public TextMeshProUGUI lobbyStatusText; // Optional: 상태 문구 표시

    private async void Start()
    {
        // 로그인 직후라면 PlayerDataManager에 값이 이미 들어있음.
        // 하지만 서버의 최신 정보를 다시 받는 것이 더 안전함(동기화).
        await RefreshUserInfoFromServer();
    }

    // 서버에서 최신 유저 정보 불러오기
    public async System.Threading.Tasks.Task RefreshUserInfoFromServer()
    {
        int userId = PlayerDataManager.Instance.userId;
        if (userId <= 0)
        {
            Debug.LogWarning("LobbyManager: 유효하지 않은 userId");
            return;
        }

        try
        {
            // 상태 표시
            if (lobbyStatusText != null) lobbyStatusText.text = "유저 정보 불러오는 중...";

            string res = await BackendManager.GetUserInfo(userId);
            JObject json = JObject.Parse(res);

            // 서버가 객체를 바로 반환하면 json["id"] 등으로 읽고,
            // 만약 배열/다른 래핑이 있다면 파싱 방식 조정 필요.
            PlayerDataManager.Instance.username = (string)json["username"];
            PlayerDataManager.Instance.level = (int)json["level"];
            PlayerDataManager.Instance.gold = (int)json["gold"];

            UpdateUI();

            if (lobbyStatusText != null) lobbyStatusText.text = "";
        }
        catch (System.Exception ex)
        {
            Debug.LogError("RefreshUserInfoFromServer error: " + ex.Message);
            if (lobbyStatusText != null) lobbyStatusText.text = "유저 정보 불러오기 실패";
        }
    }

    private void UpdateUI()
    {
        usernameText.text = PlayerDataManager.Instance.username;
        levelText.text = "Lv. " + PlayerDataManager.Instance.level.ToString();
        goldText.text = PlayerDataManager.Instance.gold.ToString();
    }

    // 버튼 이벤트들
    public void OnOpenGacha()
    {
        if (gachaPanel != null)
            gachaPanel.SetActive(true);
    }

    public void OnOpenInventory()
    {
        SceneManager.LoadScene("InventoryScene");
    }

    public void OnLogout()
    {
        // 간단 로그아웃 처리: 데이터 초기화 후 로그인 씬 이동
        PlayerDataManager.Instance.userId = 0;
        PlayerDataManager.Instance.username = "";
        PlayerDataManager.Instance.level = 0;
        PlayerDataManager.Instance.gold = 0;
        PlayerDataManager.Instance.characters.Clear();
        SceneManager.LoadScene("LoginScene");
    }

    public async void OnClickDraw()
    {
        int userId = PlayerDataManager.Instance.userId;

        string res = await BackendManager.DrawGacha(userId);
        Debug.Log("Gacha Response Raw: " + res);

        JObject json = JObject.Parse(res);

        if (json["success"].ToObject<bool>())
        {
            // 서버에서 characterId만 넘어옴
            int characterId = json["characterId"].ToObject<int>();

            Debug.Log($"⭐ 획득 캐릭터 ID: {characterId}");

            // 골드 차감 UI 반영
            PlayerDataManager.Instance.gold -= 10;
            goldText.text = PlayerDataManager.Instance.gold.ToString();

            if (lobbyStatusText != null)
                lobbyStatusText.text = $"뽑기 성공! ID: {characterId}";
        }
        else
        {
            if (lobbyStatusText != null)
                lobbyStatusText.text = json["message"].ToString();
        }
    }


}
