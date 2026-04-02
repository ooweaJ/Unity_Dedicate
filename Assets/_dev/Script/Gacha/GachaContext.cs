using System;

public static class GachaContext
{
    public static int CurrentBannerId = -1;
    public static int PullAmount = 1;
    public static GachaRewardItem[] PendingResults = null;  // 배열로 변경
    public static bool IsResultReady = false;

    public static Action OnGachaResult;

    public static void Clear()
    {
        PendingResults = null;
        IsResultReady = false;
    }
}

// 뽑기 결과 데이터 구조
[System.Serializable]
public class GachaResult
{
    public string CharacterId;
    public string CharacterName;
    public int Rarity;          // 3, 4, 5성
    public bool IsSpecialCutscene => Rarity >= 5;
}