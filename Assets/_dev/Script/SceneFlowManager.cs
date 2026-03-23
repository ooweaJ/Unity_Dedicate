using UnityEngine;
using UnityEngine.SceneManagement;
public class LoadRequest
{
    public string sceneName;
    public string serverAddress;
    public int port;
}

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance;
    public LoadRequest CurrentRequest { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Load(LoadRequest request)
    {
        CurrentRequest = request;
        SceneManager.LoadScene("LoadingScene");
    }
}