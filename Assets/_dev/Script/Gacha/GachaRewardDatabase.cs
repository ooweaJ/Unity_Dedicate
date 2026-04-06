// GachaRewardDatabase.cs
using UnityEngine;

public class GachaRewardDatabase : MonoBehaviour
{
    public static GachaRewardDatabase Instance;

    [SerializeField] private GachaRewardDatas characterTable;
    [SerializeField] private GachaRewardDatas itemTable;

    void Awake() => Instance = this;

    public GachaRewardData Find(int typeId, int rewardId)
    {
        return typeId switch
        {
            1 => characterTable.Find(rewardId),
            2 => itemTable.Find(rewardId),
            _ => null
        };
    }
}