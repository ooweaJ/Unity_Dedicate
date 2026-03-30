// 씬이 바뀌어도 데이터 유지하는 정적 컨텍스트
public static class GachaContext
{
    // 상점에서 어떤 패널 열려있었는지
    public static string LastShopPanelId = "";

    // 서버에서 받은 결과 캐시
    public static GachaResult PendingResult = null;

    // 결과 도착 여부
    public static bool IsResultReady = false;

    public static void Clear()
    {
        PendingResult = null;
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