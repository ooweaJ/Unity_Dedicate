using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIMessageType { Info, Success, Fail, Error }

public class MessagePanel : MonoBehaviour
{
    public static MessagePanel Instance { get; private set; }

    [SerializeField] private GameObject panel;       // Canvas 자식 패널 — 이것만 껐다 켬
    [SerializeField] private TMP_Text   messageText;
    [SerializeField] private Button     confirmButton;

    [Header("Colors")]
    [SerializeField] private Color colorSuccess = new Color(0.2f, 0.8f, 0.4f);
    [SerializeField] private Color colorFail    = new Color(1f,   0.3f, 0.3f);
    [SerializeField] private Color colorError   = new Color(1f,   0.6f, 0f);
    [SerializeField] private Color colorInfo    = Color.white;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        confirmButton?.onClick.AddListener(() => Hide());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Show(string message, UIMessageType type = UIMessageType.Info)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"[MessagePanel] 인스턴스 없음: {message}");
            return;
        }
        Instance.ShowInternal(message, type);
    }

    private void ShowInternal(string message, UIMessageType type)
    {
        if (messageText != null)
        {
            messageText.text  = message.Replace(". ", ".\n");
            messageText.color = type switch
            {
                UIMessageType.Success => colorSuccess,
                UIMessageType.Fail    => colorFail,
                UIMessageType.Error   => colorError,
                _                     => colorInfo
            };
        }
        panel.SetActive(true);
    }

    public static void Hide()
    {
        if (Instance == null) return;
        Instance.panel.SetActive(false);
    }
}
