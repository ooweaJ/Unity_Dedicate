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
    public GameObject modelPrefab;

    [Header("Battle")]
    public CharacterDataSO battleData;

    [Header("Base Stats")]
    public float baseHp;
    public float baseAtk;
    public float baseDef;

    [Header("Level Scaling (per level)")]
    public float atkUpgradeBonus  = 0.10f;
    public float hpUpgradeBonus   = 0.10f;
    public float defUpgradeBonus  = 0.05f;

    [Header("Game Logic")]
    public CharacterType type;
}

[CreateAssetMenu(fileName = "CharacterTable", menuName = "Data/Table/Character")]
public class CharacterTableSO : ScriptableObject, ISerializationCallbackReceiver
{
    // 1. 에디터 및 CSV용 리스트
    public List<CharacterRawData> characters = new List<CharacterRawData>();

    // 2. 실제 코드에서 사용할 고속 검색용 맵
    private Dictionary<int, CharacterRawData>          _characterMap = new();
    private Dictionary<CharacterType, CharacterRawData> _typeMap      = new();

    public CharacterRawData GetData(int id)
        => _characterMap.TryGetValue(id, out var data) ? data : null;

    public CharacterRawData GetDataByType(CharacterType type)
        => _typeMap.TryGetValue(type, out var data) ? data : null;

    // 유니티 로드 시 자동 실행
    public void OnAfterDeserialize()
    {
        _characterMap.Clear();
        _typeMap.Clear();
        foreach (var c in characters)
        {
            if (c == null) continue;
            if (!_characterMap.ContainsKey(c.id))
                _characterMap.Add(c.id, c);
            if (!_typeMap.ContainsKey(c.type))
                _typeMap.Add(c.type, c);
        }
    }

    public void OnBeforeSerialize() { }
}