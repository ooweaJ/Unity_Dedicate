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
    public string playerName;
    public int kills;
    public int deaths;
    public float damageDealt;
    public bool isWinner;
}