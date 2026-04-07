using System;

[Serializable]
public class UserCharacterData
{
    public int characterId;
    public int level;
    public int exp;
    public int enhance;
    public int shardAmount;
}

[Serializable]
public class UserItemData
{
    public int itemId;
    public int count;
}