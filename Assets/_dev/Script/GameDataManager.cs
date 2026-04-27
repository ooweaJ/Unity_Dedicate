using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [SerializeField] private GameDatabase database;

    private void Awake()
    {
        // 1. 싱글톤 및 DontDestroyOnLoad 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴 방지

            // 데이터베이스 내부의 맵(Dictionary) 초기화가 필요한 경우 여기서 수행
            // (우리가 짠 구조는 SO 로드 시 자동 초기화되므로 안전합니다)
        }
        else
        {
            Debug.LogWarning("[GameDataManager] 중복 인스턴스 파괴");
            Destroy(gameObject);
            return;
        }
    }

    // --- 편리한 데이터 접근 함수 (Getter) ---

    public ItemRawData GetItem(int id)
    {
        if (database == null || database.itemTable == null) return null;
        return database.itemTable.GetData(id);
    }

    public CharacterRawData GetCharacter(int id)
    {
        if (database == null || database.characterTable == null) return null;
        return database.characterTable.GetData(id);
    }

    public CharacterRawData GetCharacterByType(CharacterType type)
    {
        if (database == null || database.characterTable == null) return null;
        return database.characterTable.GetDataByType(type);
    }
}