using System;
using UnityEngine;

// 상속 없이 필드를 다 넣음
[Serializable]
public class GachaRewardData
{
    public int rewardId;
    public int grade;
    public string rewardName;
    public Sprite thumbnail;
    public Sprite classIcon;
    public string cutsceneKey;  // 전설만 사용
    public bool playcutscene = false;
}

[CreateAssetMenu(menuName = "Gacha/GachaRewardDatas")]
public class GachaRewardDatas : ScriptableObject
{
    [SerializeField] public GachaRewardData[] RewardDatas;

    public GachaRewardData Find(int rewardId)
        => Array.Find(RewardDatas, c => c.rewardId == rewardId);
}
