// GachaRewardData.cs
// 클라이언트에서 보상 시각 정보 관리

using UnityEngine;

public class GachaRewardData : ScriptableObject
{
    public int rewardId;
    public int typeId;
    public string rewardName;
    public Sprite thumbnail;
    public int grade;        // 1~4
}

// 캐릭터 전용 (5성 컷씬 키 추가)
[CreateAssetMenu(menuName = "Gacha/CharacterRewardData")]
public class CharacterRewardData : GachaRewardData
{
    public string cutsceneKey;  // 전설 컷씬 타임라인 key
    public Sprite classIcon;
}

// 경험치 물약
[CreateAssetMenu(menuName = "Gacha/ExpPotionRewardData")]
public class ExpPotionRewardData : GachaRewardData { }

// 초월 구슬
[CreateAssetMenu(menuName = "Gacha/OrbRewardData")]
public class OrbRewardData : GachaRewardData { }