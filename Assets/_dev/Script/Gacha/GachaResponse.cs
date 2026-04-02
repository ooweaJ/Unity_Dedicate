using System;

[Serializable]
public class GachaRewardItem
{
    public int grade;     // 1=노말 2=레어 3=에픽 4=전설
    public int typeId;    // 1=캐릭터 2=물약 3=구슬
    public int rewardId;  // 해당 타입 테이블의 ID

    public bool IsLegendary => grade == 4;
}

[Serializable]
public class GachaPullResponse
{
    public bool success;
    public GachaRewardItem[] results;
}