using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private CharacterSlot slotPrefab;

    private void Start()
    {
        for (int i = 0; i < 30; i++)
        {
            Instantiate(slotPrefab, content);
        }
    }

}
