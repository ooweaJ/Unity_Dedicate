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

    private StatUtils.CalculatedStats _stats =>
        StatUtils.Calculate(StaticData, ServerData.level, ServerData.equippedItems.Values);

    public int StarRating => StaticData.grade;
    public string Name => StaticData.displayName;
    public string LevelText => ServerData.level.ToString() + "/";
    public float ExpProgress => (float)ServerData.exp;
    public string Hp  => ((int)_stats.hp).ToString();
    public string Atk => ((int)_stats.atk).ToString();
    public string Def => ((int)_stats.def).ToString();
    public string TranscendText => $"+{ServerData.enhance}";
    public Sprite Icon => StaticData.icon;
    public int CharacterId => StaticData.id;
}