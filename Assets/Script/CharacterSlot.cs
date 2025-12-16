using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI levelText;

    private UserCharacter userCharacter;

    // 🔹 테스트용
    public void SetDummy(int index)
    {
        levelText.text = $"Lv.{index}";
        icon.color = Random.ColorHSV(); // 잘 보이게
    }
}
