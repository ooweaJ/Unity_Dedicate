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

    void Start()
    {
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

            // 90%까지는 실제 진행도
            if (op.progress < 0.9f)
            {
                timer += Time.deltaTime;
                progressBarFill.fillAmount = Mathf.Lerp(progressBarFill.fillAmount, op.progress, timer);
                percentText.text = $"{(int)(progressBarFill.fillAmount * 100)}%";
            }
            // 90% 이후엔 100%까지 부드럽게
            else
            {
                timer += Time.deltaTime;
                progressBarFill.fillAmount = Mathf.Lerp(progressBarFill.fillAmount, 1f, timer);
                percentText.text = $"{(int)(progressBarFill.fillAmount * 100)}%";

                if (progressBarFill.fillAmount >= 0.99f)
                {
                    op.allowSceneActivation = true;
                }
            }
        }
    }
}