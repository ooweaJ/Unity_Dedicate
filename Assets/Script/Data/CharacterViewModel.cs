public class CharacterViewModel
{
    public CharacterData baseData;
    public PlayerCharacterData playerData;

    public CharacterViewModel(CharacterData baseData, PlayerCharacterData playerData)
    {
        this.baseData = baseData;
        this.playerData = playerData;
    }

    public string Name => baseData.name;
    public int Rarity => baseData.rarity;
    public int Level => playerData.level;

    public int FinalAtk =>
        baseData.baseAtk + playerData.level * 5 + playerData.upgrade * 10;

    public int FinalHp =>
        baseData.baseHp + playerData.level * 20;
}
