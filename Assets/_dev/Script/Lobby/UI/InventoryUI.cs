using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private CharacterSlot slotPrefab;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private GameObject panel;

    public void Init(IEnumerable<PlayerCharacterData> playerCharacterDatas)
    {
        foreach (var pc in playerCharacterDatas)
        {
            CharacterData baseData =
                characterDatabase.GetById(pc.characterId);

            CharacterSlot slot = Instantiate(slotPrefab, content);
            slot.GetComponent<CharacterSlot>().Set(pc, baseData);
        }
    }

    public void Refresh(IEnumerable<PlayerCharacterData> playerCharacterDatas)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var pc in playerCharacterDatas)
        {
            var baseData = characterDatabase.GetById(pc.characterId);
            var slot = Instantiate(slotPrefab, content);
            slot.Set(pc, baseData);
        }
    }

    public void Open()
    {
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }
}
