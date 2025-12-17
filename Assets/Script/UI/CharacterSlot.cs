using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI levelText;

    private UserCharacter userCharacter;

    public void Set(PlayerCharacterData pc, CharacterData data)
    {
        icon.sprite = data.icon;
        levelText.text = $"Lv.{pc.level}";
    }
}
