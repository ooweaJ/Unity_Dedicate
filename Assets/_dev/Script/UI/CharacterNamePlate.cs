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

    // ── 외부에서 이걸 호출 ────────────────────────────
    public void Setup(CharacterNamePlateData data)
    {
        // 아이콘
        classIconImage.sprite = data.classIcon;
        iconBoxBackground.color = data.iconBoxColor;

        // 이름
        nameText.text = data.characterName;

        // 별 개수
        for (int i = 0; i < stars.Length; i++)
            stars[i].SetActive(i < data.starCount);

        // UP 뱃지
        upBadge.SetActive(data.showUpBadge);
    }
}