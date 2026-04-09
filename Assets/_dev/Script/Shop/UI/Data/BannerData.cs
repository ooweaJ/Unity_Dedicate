// BannerData.cs

using UnityEngine;

[CreateAssetMenu(fileName = "BannerData", menuName = "Gacha/BannerData")]
public class BannerData : ScriptableObject
{
    [Header("배너 식별자")]
    public int bannerId;

    [Header("왼쪽 리스트")]
    public Sprite thumbnailSprite;      // 리스트 썸네일

    [Header("메인 콘텐츠")]
    public Sprite illustrationSprite;  // 뽑기 일러스트 (배경)
    public string bannerTitle;
    public string remainTime;

    [Header("네임카드")]
    public string characterName;
    public Sprite classIcon;
    public int starCount;            // 3, 4, 5
}