using UnityEngine;

[CreateAssetMenu(fileName = "NamePlateData", menuName = "Gacha/NamePlateData")]
public class CharacterNamePlateData : ScriptableObject
{
    public string characterName;
    public Sprite classIcon;
    public Color iconBoxColor = new Color(0.2f, 0.1f, 0.3f, 1f);
    public int starCount;         // 3, 4, 5
    public bool showUpBadge;
}

[CreateAssetMenu(fileName = "BannerData", menuName = "Gacha/BannerData")]
public class BannerData : ScriptableObject
{
    [Header("배너 기본 정보")]
    public string bannerId;           // "event_001"
    public string bannerTitle;        // "실추되지 않는 태양관"
    public string badgeText;          // "이벤트", "자유 선택"
    public string remainTimeText;     // "남은 시간: 19일 20시간"

    [Header("설명")]
    [TextArea(3, 6)]
    public string descriptionText;

    [Header("이미지")]
    public Sprite thumbnailSprite;    // 왼쪽 리스트 썸네일
    public Sprite backgroundSprite;  // 메인 배경 이미지
    public Sprite characterSprite;   // 캐릭터 일러스트 (선택)

    [Header("뽑기 설정")]
    public int costSingle = 1;
    public int costTen = 10;
    public string currencyType = "star_dust";  // 재화 종류

    [Header("캐릭터 네임플레이트")]
    public CharacterNamePlateData namePlateData;
}