using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterUIModel
{
    public PlayerCharacterData ServerData { get; private set; }
    public CharacterRawData StaticData { get; private set; }

    public CharacterUIModel(PlayerCharacterData serverData, CharacterRawData staticData)
    {
        ServerData = serverData;
        StaticData = staticData;
    }

    // UI에서 접근하기 편하게 가공된 데이터 제공
    public int StarRating => StaticData.grade; // CharacterSO에 성급 데이터 추가 필요
    public string Name => StaticData.displayName;
    public string LevelText => ServerData.level.ToString() + "/"; // SO에 maxLevel 추가 필요
    public float ExpProgress => (float)ServerData.exp; // SO에 expToNextLevel 추가 필요
    public string Hp  => StaticData.baseHp.ToString();
    public string Atk => (StaticData.baseAtk + GetEquipmentBonus(e => e.atkBonus)).ToString();
    public string Def => (StaticData.baseDef + GetEquipmentBonus(e => e.defBonus)).ToString();

    private int GetEquipmentBonus(System.Func<ItemRawData, int> selector)
    {
        int total = 0;
        foreach (var itemId in ServerData.equippedItems.Values)
        {
            var item = GameDataManager.Instance.GetItem(itemId);
            if (item != null) total += selector(item);
        }
        return total;
    }
    public string TranscendText => $"+{ServerData.enhance}"; // BMP Color Tag 없이 넘기는 방식입니다.
    public Sprite Icon => StaticData.icon;
    public int CharacterId => StaticData.id;
}