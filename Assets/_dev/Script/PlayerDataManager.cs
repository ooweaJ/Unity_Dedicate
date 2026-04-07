using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // 실제 데이터 뭉치
    private PlayerData _data = new PlayerData();
    public System.Action OnDataUpdated;

    [Header("Session Data")]
    [SerializeField] private CharacterType selectedCharacter = CharacterType.Swordsman;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task RefreshMyData()
    {
        // 1. 통신은 BackendManager에게 시킴
        string json = await BackendManager.GetUserInfo(_data.userId);
        JObject response = JObject.Parse(json);

        if (response["success"] != null && (bool)response["success"])
        {
            // 2. 내 데이터를 스스로 업데이트
            this.ApplyUserData(response);
            Debug.Log("내 정보 동기화 완료!");
        }
    }

    // 서버 데이터를 받았을 때 호출
    public void ApplyUserData(JToken data)
    {
        _data.Apply(data);
        OnDataUpdated?.Invoke();
        Debug.Log($"[PlayerDataManager] Data Applied for: {GetUsername()}");
    }

    public void ClearData() => _data.Clear();

    // 캐릭터 선택 비즈니스 로직
    public void SelectCharacter(CharacterType type)
    {
        selectedCharacter = type;
        Debug.Log($"[PlayerDataManager] 캐릭터 선택 변경: {type}");
    }

    // 외부 참조용 Getter
    public CharacterType GetSelectedCharacter() => selectedCharacter;
    public int GetUserId() => _data.userId;
    public string GetUsername() => _data.username;
    public int GetLevel() => _data.level;
    public int GetGold() => _data.gold;
    public PlayerInventory GetInventory() => _data.inventory;

    // 편의용 메서드
    public void ConsumeGold(int amount) => _data.ConsumeGold(amount);
    public void CharacterAddOrUpdate(PlayerCharacterData charData) => _data.inventory.AddOrUpdateCharacter(charData);
}