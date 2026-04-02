using System.Collections.Generic;
using UnityEngine;

public class GachaRewardDatabase : MonoBehaviour
{
    public static GachaRewardDatabase Instance;

    [SerializeField] private List<CharacterRewardData> characters;
    [SerializeField] private List<ExpPotionRewardData> expPotions;
    [SerializeField] private List<OrbRewardData> orbs;

    void Awake() => Instance = this;

    public GachaRewardData Find(int typeId, int rewardId)
    {
        return typeId switch
        {
            1 => characters.Find(c => c.rewardId == rewardId),
            2 => expPotions.Find(p => p.rewardId == rewardId),
            3 => orbs.Find(o => o.rewardId == rewardId),
            _ => null
        };
    }
}