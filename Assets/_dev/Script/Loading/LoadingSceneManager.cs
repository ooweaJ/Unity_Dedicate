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
    }

    IEnumerator LoadWithServer(LoadRequest req)
    {
        yield return null;
        float minDuration = 1.0f;
        float startFill = 0.0f;
        // 0 → 30% 1초 동안 (최소 로딩 연출)
        float timer = 0f;
        while (timer < 1f)
        {
            yield return null;
            timer += Time.deltaTime;
            loadingUI.ProgressBarFill.fillAmount = Mathf.Lerp(startFill, 0.3f, timer);
            loadingUI.PercentText.text = $"{(int)(loadingUI.ProgressBarFill.fillAmount * 100)}%";
        }

        // 30 → 80% 씬 로드와 함께
        AsyncOperation op = SceneManager.LoadSceneAsync(req.sceneName);
        op.allowSceneActivation = false;

        timer = 0f;
        startFill = 0.3f;
        while (op.progress < 0.9f || timer < minDuration )
        {
            yield return null;
            timer += Time.deltaTime;
            float progress = Mathf.InverseLerp(0f, 0.9f, op.progress);
            loadingUI.ProgressBarFill.fillAmount = Mathf.Lerp(startFill, 0.6f, progress);
            loadingUI.PercentText.text = $"{(int)(loadingUI.ProgressBarFill.fillAmount * 100)}%";
        }

        // 서버 연결
        var transport = CustomNetworkManager.Instance.GetComponent<kcp2k.KcpTransport>();
        transport.port = (ushort)req.port;
        CustomNetworkManager.Instance.networkAddress = req.serverAddress;
        CustomNetworkManager.Instance.StartClient();

        startFill = 0.6f;
        timer = 0f;
        const float connectTimeout = 5f;
        while (!Mirror.NetworkClient.isConnected && timer < connectTimeout)
        {
            yield return null;
            timer += Time.deltaTime;
            loadingUI.ProgressBarFill.fillAmount = Mathf.Lerp(startFill, 0.9f, timer / connectTimeout);
            loadingUI.PercentText.text = $"{(int)(loadingUI.ProgressBarFill.fillAmount * 100)}%";
        }

        if (!Mirror.NetworkClient.isConnected)
        {
            Debug.LogError($"[LOADING] 서버 연결 실패: {req.serverAddress}:{req.port}");
            Mirror.NetworkClient.Disconnect();
            SceneManager.LoadScene("LoginScene");
            yield break;
        }

        timer = 0f;
        while (timer < 1.0f)
        {
            yield return null;
            timer += Time.deltaTime;
            loadingUI.ProgressBarFill.fillAmount = Mathf.Lerp(0.9f, 1.0f, timer);
            loadingUI.PercentText.text = $"{(int)(loadingUI.ProgressBarFill.fillAmount * 100)}%";
        }

        NetworkClient.Ready();
        NetworkClient.AddPlayer();
        op.allowSceneActivation = true;
        yield return op;
    }
}