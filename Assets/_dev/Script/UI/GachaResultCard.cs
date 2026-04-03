
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaResultCard : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI resultTypeText; // "신규" / "초월" / "획득"
    [SerializeField] private Image gradeFrame;     // 등급별 테두리 색

    [Header("등급 색상")]
    [SerializeField] private Color colorNormal = Color.gray;
    [SerializeField] private Color colorRare = Color.green;
    [SerializeField] private Color colorEpic = Color.magenta;
    [SerializeField] private Color colorLegendary = Color.yellow;

    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
    }

    public void Setup(GachaRewardData data, int grade, string resultType)
    {
        thumbnailImage.sprite = data.thumbnail;
        nameText.text = data.rewardName;
        gradeText.text = GradeToString(grade);
        gradeFrame.color = GradeToColor(grade);

        resultTypeText.text = resultType switch
        {
            "character" => "신규 획득",
            "enhance" => "초월",
            "item" => "획득",
            _ => ""
        };
    }

    // 카드 등장 연출 (페이드 인)
    public void PlayReveal()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }

    string GradeToString(int grade) => grade switch
    {
        1 => "노말",
        2 => "레어",
        3 => "에픽",
        4 => "전설",
        _ => ""
    };

    Color GradeToColor(int grade) => grade switch
    {
        1 => colorNormal,
        2 => colorRare,
        3 => colorEpic,
        4 => colorLegendary,
        _ => Color.white
    };
}