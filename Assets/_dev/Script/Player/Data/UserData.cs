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

[Serializable]
public class UserInfoResponse
{
    public bool success;
    public int id;
    public string username;
    public int gold;
    public UserCharacterData[] characters;
    public UserItemData[] items;
}