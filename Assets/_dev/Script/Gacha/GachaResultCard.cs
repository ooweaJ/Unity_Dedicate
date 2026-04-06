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

    [Header("등급 색상")]
    [SerializeField] private Color colorNormal = Color.gray;
    [SerializeField] private Color colorRare = Color.green;
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
        gradeFrame.color = GradeToColor(item.grade);
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

    Color GradeToColor(int grade) => grade switch
    {
        1 => colorNormal,
        2 => colorRare,
        3 => colorEpic,
        4 => colorLegendary,
        _ => Color.white
    };
}