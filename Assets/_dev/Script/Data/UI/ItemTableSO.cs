using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Consumable,     // 소모품
    Equipment,      // 장비
    Material,       // 재료
    Transcendence,  // 초월 (기존 유지)
    TranscendShard  // 초월 조각
}

public enum EquipmentSlotType 
{ 
    Weapon, 
    Armor, 
    Accessory, 
    Ring 
}

public enum ItemActionType 
{ 
    Use, 
    Discard, 
    OpenTranscendence 
}

[System.Serializable]
public class ItemActionDef
{
    public string label;        // "사용", "버리기"
    public ItemActionType type;
}

[System.Serializable]
public class ItemRawData
{
    public int id;
    public ItemType itemType;
    public string displayName;
    public Sprite icon;
    public int maxStack;
    [TextArea(3, 10)]
    public string description;

    [Header("Actions")]
    public List<ItemActionDef> actions; // 소모품/재료용 클릭 액션

    [Header("Equipment Stats (Only for Equipment)")]
    public EquipmentSlotType slotType;
    public int atkBonus;
    public int defBonus;
}

[CreateAssetMenu(fileName = "ItemTable", menuName = "Data/Table/Item")]
public class ItemTableSO : ScriptableObject, ISerializationCallbackReceiver
{
    public List<ItemRawData> items = new List<ItemRawData>();
    private Dictionary<int, ItemRawData> _itemMap = new();

    public ItemRawData GetData(int id)
    {
        return _itemMap.TryGetValue(id, out var data) ? data : null;
    }

    public void OnAfterDeserialize()
    {
        _itemMap.Clear();
        foreach (var i in items)
        {
            if (i != null && !_itemMap.ContainsKey(i.id))
                _itemMap.Add(i.id, i);
        }
    }

    public void OnBeforeSerialize() { }
}