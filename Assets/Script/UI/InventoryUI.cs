using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private CharacterSlot slotPrefab;
    [SerializeField] private CharacterDatabase characterDatabase;

    private void Start()
    {
        foreach (var pc in PlayerDataManager.Instance.inventory.GetAll())
        {
            CharacterData baseData =
                characterDatabase.GetById(pc.characterId);

            CharacterSlot slot = Instantiate(slotPrefab, content);
            slot.GetComponent<CharacterSlot>().Set(pc, baseData);
        }
    }

}
