using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemData
{
    public int id;
    public string name;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items;
    private Dictionary<int, ItemData> itemMap;

    public void Init()
    {
        itemMap = new Dictionary<int, ItemData>();
        foreach (var item in items)
        {
            itemMap[item.id] = item;
        }
    }

    public ItemData GetById(int id)
    {
        if (itemMap == null) Init(); // 방어 코드
        if (itemMap.TryGetValue(id, out var data)) return data;
        return null;
    }
}