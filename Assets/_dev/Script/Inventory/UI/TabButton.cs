using UnityEngine;
using UnityEngine.UI;
using System;

[System.Serializable]
public struct TabPanelMapping // 에디터에서 연결하기 위한 구조체
{
    public int tabId;
    public GameObject panel;
}
public class TabButton : MonoBehaviour
{
    public int tabId;
    public Button button;
    public Image iconImage;

    public Action<TabButton> OnTabClicked;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        iconImage = GetComponent<Image>();
        button.onClick.AddListener(() => OnTabClicked?.Invoke(this));
    }

    public void SetSelect(bool isSelected, Color goldColor)
    {
        if (iconImage != null)
        {
            // 선택되면 이쁜 황금색, 아니면 흰색(또는 회색)
            iconImage.color = isSelected ? goldColor : Color.white;
        }
    }
}