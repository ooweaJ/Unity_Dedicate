using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Mirror;

public class LoadingSceneManager : MonoBehaviour
{
    public static LoadingSceneManager Instance;

    [SerializeField] private LoadingUI loadingUI;

    private string[] tips = {
        "던전에서 스킬 타이밍이 승패를 결정합니다!",
        "상대방의 패턴을 파악하세요!",
        "캐릭터마다 고유한 스킬이 있습니다!",
        "매칭 후 빠르게 포지션을 잡으세요!"
    };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var req = SceneFlowManager.Instance.CurrentRequest;

        loadingUI.TipText.text = tips[Random.Range(0, tips.Length)];

        if (!string.IsNullOrEmpty(req.serverAddress))
            StartCoroutine(LoadWithServer(req));
        else
            StartCoroutine(LoadScene(req.sceneName));
    }

    IEnumerator LoadScene(string sceneName)
    {
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            float target = op.progress < 0.9f ? op.progress : 1f;
            loadingUI.ProgressBarFill.fillAmount = Mathf.Lerp(loadingUI.ProgressBarFill.fillAmount, target, timer);
            loadingUI.PercentText.text = $"{(int)(loadingUI.ProgressBarFill.fillAmount * 100)}%";

            if (loadingUI.ProgressBarFill.fillAmount >= 0.99f)
                op.allowSceneActivation = true;
        }
    }
    IEnumerator LoadWithServer(LoadRequest req)
    {
        // -------------------------
        // 1. 0 ~ 50% (연출)
        // -------------------------
        while (loadingUI.ProgressBarFill.fillAmount < 0.5f)
        {
            yield return null;
            loadingUI.ProgressBarFill.fillAmount =
                Mathf.MoveTowards(loadingUI.ProgressBarFill.fillAmount, 0.5f, Time.deltaTime * 0.3f);
            loadingUI.PercentText.text =
                $"{(int)(loadingUI.ProgressBarFill.fillAmount * 100)}%";
        }
        // -------------------------
        // 2. 서버 연결
        // -------------------------
        bool isConnected = false;
        CustomNetworkManager.Instance.OnClientConnected += () =>
        {
            isConnected = true;
        };
        var transport = CustomNetworkManager.Instance.GetComponent<kcp2k.KcpTransport>();
        transport.port = (ushort)req.port;
        CustomNetworkManager.Instance.networkAddress = req.serverAddress;
        CustomNetworkManager.Instance.StartClient();
        // 50%에서 대기
        while (!isConnected)
            yield return null;
        // -------------------------
        // 3. 씬 로딩 시작
        // -------------------------
        AsyncOperation op = SceneManager.LoadSceneAsync(req.sceneName);
        op.allowSceneActivation = false;
        // -------------------------
        // 4. 50 ~ 90% (진짜 로딩)
        // -------------------------
        while (op.progress < 0.9f)
        {
            yield return null;
            float target = 0.5f + op.progress * 0.4f;
            loadingUI.ProgressBarFill.fillAmount =
                Mathf.MoveTowards(loadingUI.ProgressBarFill.fillAmount, target, Time.deltaTime);
            loadingUI.PercentText.text =
                $"{(int)(loadingUI.ProgressBarFill.fillAmount * 100)}%";
        }
        // -------------------------
        // 5. 90 ~ 100%
        // -------------------------
        while (loadingUI.ProgressBarFill.fillAmount < 1f)
        {
            yield return null;
            loadingUI.ProgressBarFill.fillAmount =
                Mathf.MoveTowards(loadingUI.ProgressBarFill.fillAmount, 1f, Time.deltaTime * 0.7f);
            loadingUI.PercentText.text =
                $"{(int)(loadingUI.ProgressBarFill.fillAmount * 100)}%";
        }
        yield return new WaitForSeconds(0.3f);
        op.allowSceneActivation = true;
    }
}