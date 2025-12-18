using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private CharacterSlot slotPrefab;
    [SerializeField] private CharacterDatabase characterDatabase;

    private void OnEnable()
    {
        PlayerDataManager.Instance.inventory.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        PlayerDataManager.Instance.inventory.OnChanged -= Refresh;
    }

    void Refresh()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var pc in PlayerDataManager.Instance.inventory.GetAll())
        {
            var baseData = characterDatabase.GetById(pc.characterId);
            var slot = Instantiate(slotPrefab, content);
            slot.Set(pc, baseData);
        }
    }

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
