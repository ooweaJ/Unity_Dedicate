public class CharacterUIModel
{
    public PlayerCharacterData ServerData { get; private set; }
    public CharacterRawData    StaticData { get; private set; }

    // 초월 1단계당 최대 레벨 +2, 기본 최대 30 → 5초월 시 최대 40
    private const int BASE_MAX_LEVEL         = 30;
    private const int LEVEL_BONUS_PER_TRANSCEND = 2;

    public CharacterUIModel(PlayerCharacterData serverData, CharacterRawData staticData)
    {
        ServerData = serverData;
        StaticData = staticData;
    }

    private StatUtils.CalculatedStats Stats =>
        StatUtils.Calculate(StaticData, ServerData.level, ServerData.equippedItems.Values);

    public int    MaxLevel      => BASE_MAX_LEVEL + ServerData.enhance * LEVEL_BONUS_PER_TRANSCEND;
    public int    StarRating    => StaticData.grade;
    public string Name          => StaticData.displayName;
    public string LevelText     => $"{ServerData.level}/{MaxLevel}";
    public float  ExpProgress   => (float)ServerData.exp;
    public string Hp            => ((int)Stats.hp).ToString();
    public string Atk           => ((int)Stats.atk).ToString();
    public string Def           => ((int)Stats.def).ToString();
    public string TranscendText => $"+{ServerData.enhance}";
    public UnityEngine.Sprite Icon => StaticData.icon;
    public int    CharacterId   => StaticData.id;
}
