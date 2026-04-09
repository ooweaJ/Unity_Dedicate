using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemRawData
{
    public int id;
    public string displayName;
    public Sprite icon;
    public int maxStack;
    [TextArea(3, 10)]
    public string description;
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