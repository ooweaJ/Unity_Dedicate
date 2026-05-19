/// <summary>
/// 전투 결과에 따른 EXP / 랭크포인트 계산
/// 수치 튜닝은 이 파일 상수만 수정
/// </summary>
public static class ExpCalculator
{
    private const int   WinBaseExp   = 150;
    private const int   LoseBaseExp  = 50;
    private const int   DrawBaseExp  = 80;
    private const int   ExpPerKill   = 30;
    private const float ExpPerDamage = 10f; // 10 딜 = 1 EXP

    private const int WinRank  =  20;
    private const int LoseRank = -15;
    private const int DrawRank =   5;

    private const int WinBaseGold   = 200;
    private const int LoseBaseGold  = 80;
    private const int DrawBaseGold  = 120;
    private const int GoldPerKill   = 20;

    public static int Calculate(PlayerResultData data, bool isDraw)
    {
        int exp = isDraw ? DrawBaseExp : (data.isWinner ? WinBaseExp : LoseBaseExp);
        exp += data.kills * ExpPerKill;
        exp += (int)(data.damageDealt / ExpPerDamage);
        return exp;
    }

    public static int GoldCalculate(PlayerResultData data, bool isDraw)
    {
        int gold = isDraw ? DrawBaseGold : (data.isWinner ? WinBaseGold : LoseBaseGold);
        gold += data.kills * GoldPerKill;
        return gold;
    }

    public static int RankDelta(bool isWinner, bool isDraw)
    {
        if (isDraw) return DrawRank;
        return isWinner ? WinRank : LoseRank;
    }
}
