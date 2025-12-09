using UnityEngine;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        // PlayerDataManager, NetworkManager 모두 이미 이 오브젝트에 붙어있다 = 객체 존재 보장됨

        SceneManager.LoadScene("LoginScene");
    }
}
