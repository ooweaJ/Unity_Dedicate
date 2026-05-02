using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    private PlayerData _data = new PlayerData();
    public System.Action OnDataUpdated;

    [Header("Session Data")]
    [SerializeField] private CharacterType selectedCharacter;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task RefreshMyData()
    {
        string json = await BackendManager.GetUserInfo(_data.userId);
        JObject response = JObject.Parse(json);

        if (response["success"] != null && (bool)response["success"])
        {
            this.ApplyUserData(response);
            Debug.Log("내 정보 동기화 완료!");
        }
    }

    public void ApplyUserData(JToken data)
    {
        _data.Apply(data);
        ValidateSelectedCharacter();
        OnDataUpdated?.Invoke();
        Debug.Log($"[PlayerDataManager] Data Applied for: {GetUsername()}");
    }

    private void ValidateSelectedCharacter()
    {
        var ownedChars = _data.inventory.GetAllCharacters().ToList();
        if (ownedChars.Count == 0) return;

        // 1. 서버에 저장된 선택 캐릭터 복원 시도
        if (_data.selectedCharacterId > 0)
        {
            var savedChar = ownedChars.FirstOrDefault(c => c.characterId == _data.selectedCharacterId);
            if (savedChar != null)
            {
                var staticData = GameDataManager.Instance.GetCharacter(savedChar.characterId);
                if (staticData != null)
                {
                    selectedCharacter = staticData.type;
                    return;
                }
            }
        }

        // 2. 현재 선택이 여전히 유효한지 확인
        bool isCurrentOwned = ownedChars.Any(c =>
        {
            var s = GameDataManager.Instance.GetCharacter(c.characterId);
            return s != null && s.type == selectedCharacter;
        });

        // 3. 없으면 첫 번째 캐릭터로 fallback
        if (!isCurrentOwned)
        {
            var firstChar  = ownedChars[0];
            var staticData = GameDataManager.Instance.GetCharacter(firstChar.characterId);
            if (staticData != null)
                selectedCharacter = staticData.type;
        }
    }

    public void ClearData() => _data.Clear();

    // 캐릭터 선택 — 로컬 세팅 + 서버 저장
    public async void SelectCharacter(CharacterType type)
    {
        selectedCharacter = type;

        var charRaw = GameDataManager.Instance.GetCharacterByType(type);
        if (charRaw == null) return;

        try
        {
            await BackendManager.SelectCharacter(_data.userId, charRaw.id);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerDataManager] SelectCharacter API 실패: {e.Message}");
        }
    }

    // ── Getter ──────────────────────────────────────────────────────────
    public CharacterType   GetSelectedCharacter()   => selectedCharacter;
    public int             GetUserId()              => _data.userId;
    public string          GetUsername()            => _data.username;
    public int             GetLevel()               => _data.level;
    public int             GetExp()                 => _data.exp;
    public int             GetGold()                => _data.gold;
    public int             GetSelectedCharacterId() => _data.selectedCharacterId;
    public PlayerInventory GetInventory()           => _data.inventory;

    public void ConsumeGold(int amount) => _data.ConsumeGold(amount);
    public void CharacterAddOrUpdate(PlayerCharacterData charData) => _data.inventory.AddOrUpdateCharacter(charData);
}
