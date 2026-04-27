using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // 실제 데이터 뭉치
    private PlayerData _data = new PlayerData();
    public System.Action OnDataUpdated;

    [Header("Session Data")]
    [SerializeField] private CharacterType selectedCharacter;

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
        
        // 보유한 캐릭터가 있는데 현재 선택된 캐릭터가 없거나 무효하다면 첫 번째 캐릭터 자동 선택
        ValidateSelectedCharacter();

        OnDataUpdated?.Invoke();
        Debug.Log($"[PlayerDataManager] Data Applied for: {GetUsername()}");
    }

    private void ValidateSelectedCharacter()
    {
        var ownedChars = _data.inventory.GetAllCharacters().ToList();
        if (ownedChars.Count == 0) return;

        // 현재 선택된 캐릭터가 보유 목록에 있는지 확인
        bool isCurrentOwned = ownedChars.Any(c => 
        {
            var staticData = GameDataManager.Instance.GetCharacter(c.characterId);
            return staticData != null && staticData.type == selectedCharacter;
        });

        // 없으면 첫 번째 보유 캐릭터로 강제 설정
        if (!isCurrentOwned)
        {
            var firstChar = ownedChars[0];
            var staticData = GameDataManager.Instance.GetCharacter(firstChar.characterId);
            if (staticData != null)
            {
                SelectCharacter(staticData.type);
            }
        }
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