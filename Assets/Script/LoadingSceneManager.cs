using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingSceneManager : MonoBehaviour
{
    public static string nextScene;

    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private TextMeshProUGUI tipText;

    private string[] tips = {
        "던전에서 스킬 타이밍이 승패를 결정합니다!",
        "상대방의 패턴을 파악하세요!",
        "캐릭터마다 고유한 스킬이 있습니다!",
        "매칭 후 빠르게 포지션을 잡으세요!"
    };

    void Start()
    {
        tipText.text = tips[Random.Range(0, tips.Length)];
        StartCoroutine(LoadScene());
    }

    IEnumerator LoadScene()
    {
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;

        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            if (op.progress < 0.9f)
            {
                progressBarFill.fillAmount = Mathf.Lerp(progressBarFill.fillAmount, op.progress, timer);
            }
            else
            {
                progressBarFill.fillAmount = Mathf.Lerp(progressBarFill.fillAmount, 1f, timer);

                if (progressBarFill.fillAmount >= 0.99f)
                    op.allowSceneActivation = true;
            }

            percentText.text = $"{(int)(progressBarFill.fillAmount * 100)}%";
        }
    }
}