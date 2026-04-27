using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterRawData
{
    [Header("Basic Info")]
    public int id;
    public string displayName;
    public int grade = 1;
    public Sprite icon;             // 본체 초상화
    public Sprite shardIcon;        // 초월용 조각 아이콘
    [TextArea] public string description;

    [Header("Visuals")]
    public GameObject modelPrefab; // 실제 필드/전투에서 소환할 프리팹

    [Header("Base Stats")]
    public float baseHp;
    public float baseAtk;
    public float baseDef;

    [Header("Game Logic")]
    public CharacterType type; 
}

[CreateAssetMenu(fileName = "CharacterTable", menuName = "Data/Table/Character")]
public class CharacterTableSO : ScriptableObject, ISerializationCallbackReceiver
{
    // 1. 에디터 및 CSV용 리스트
    public List<CharacterRawData> characters = new List<CharacterRawData>();

    // 2. 실제 코드에서 사용할 고속 검색용 맵
    private Dictionary<int, CharacterRawData> _characterMap = new();

    // ID로 데이터 가져오기 (O(1) 성능)
    public CharacterRawData GetData(int id)
    {
        return _characterMap.TryGetValue(id, out var data) ? data : null;
    }

    // 유니티 로드 시 자동 실행
    public void OnAfterDeserialize()
    {
        _characterMap.Clear();
        foreach (var c in characters)
        {
            if (c != null && !_characterMap.ContainsKey(c.id))
                _characterMap.Add(c.id, c);
        }
    }

    public void OnBeforeSerialize() { }
}