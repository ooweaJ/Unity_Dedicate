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
        // PlayerDataManager, NetworkManager ��� �̹� �� ������Ʈ�� �پ��ִ� = ��ü ���� �����

        SceneManager.LoadScene("LoginScene");
    }
}
