using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIMessageType { Info, Success, Fail, Error }

public class MessagePanel : MonoBehaviour
{
    public static MessagePanel Instance { get; private set; }

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button   confirmButton;

    [Header("Colors")]
    [SerializeField] private Color colorSuccess = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color colorFail    = new Color(1f,   0.2f, 0.2f);
    [SerializeField] private Color colorError   = new Color(1f,   0.5f, 0f);
    [SerializeField] private Color colorInfo    = Color.white;

    private void Awake()
    {
        Instance = this;
        confirmButton?.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(string message, UIMessageType type = UIMessageType.Info)
    {
        if (messageText != null)
        {
            messageText.text  = message;
            messageText.color = type switch
            {
                UIMessageType.Success => colorSuccess,
                UIMessageType.Fail    => colorFail,
                UIMessageType.Error   => colorError,
                _                     => colorInfo
            };
        }
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
