// GachaResultCard.cs — 카드 프리팹에 붙임
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GachaResultCard : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image gradeFrame;
    [SerializeField] private Image gradeBackground;
    [SerializeField] private Sprite[] gradeFrames;

    [Header("등급 색상")]
    [SerializeField] private Color colorNormal = Color.green;
    [SerializeField] private Color colorRare = Color.blue;
    [SerializeField] private Color colorEpic = Color.magenta;
    [SerializeField] private Color colorLegendary = Color.yellow;

    private CanvasGroup _canvasGroup;

    void Awake()
    {

    }

    // GachaRewardItem 받아서 SO 직접 찾아 세팅
    public void Setup(GachaRewardItem item)
    {
        var data = GachaRewardDatabase.Instance.Find(item.typeId, item.rewardId);
        if (data == null) return;

        thumbnailImage.sprite = data.thumbnail;
        nameText.text = data.rewardName;
        GradeToFrame(data.grade);
    }

    public IEnumerator PlayReveal()
    {
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }

    void GradeToFrame(int grade)
    {
        switch(grade)
        {
            case 1:
                gradeBackground.color = colorNormal;
                gradeFrame.sprite = gradeFrames[grade-1];
                break;
            case 2:
                gradeBackground.color = colorRare;
                gradeFrame.sprite = gradeFrames[grade - 1];
                break;
            case 3:
                gradeBackground.color = colorEpic;
                gradeFrame.sprite = gradeFrames[grade - 1];
                break;
            case 4:
                gradeBackground.color = colorLegendary;
                gradeFrame.sprite = gradeFrames[grade - 1];
                break;
        }
    }
}