using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterNamePlate : MonoBehaviour
{
    [Header("아이콘")]
    [SerializeField] private Image classIconImage;
    [SerializeField] private Image iconBoxBackground;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("별 (최대 5개 미리 만들어둠)")]
    [SerializeField] private GameObject[] stars;   // 인스펙터에서 5개 연결

    [Header("UP 뱃지")]
    [SerializeField] private GameObject upBadge;

    public void Setup(BannerData data)
    {
        nameText.text = data.characterName;
        classIconImage.sprite = data.classIcon;

        for (int i = 0; i < stars.Length; i++)
            stars[i].SetActive(i < data.starCount);
    }
}