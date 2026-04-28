using System.Collections.Generic;

[System.Serializable]
public class BattleResultData
{
    public string winnerName;
    public bool isDraw;

    // 플레이어별 전적
    public List<PlayerResultData> playerResults = new List<PlayerResultData>();
}

[System.Serializable]
public class PlayerResultData
{
    public uint   netId;          // Mirror 고유 ID — 로컬 플레이어 식별용
    public string playerName;     // 닉네임 (표시용)
    public int    kills;
    public int    deaths;
    public float  damageDealt;
    public bool   isWinner;
    public int    expGained;
    public int    rankPointDelta;
}