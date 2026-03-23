using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryUI inventoryUI;
    
    private PlayerInventory inventory;

    void Start()
    {
        Init();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnChanged -= HandleRefresh;
    }

    void Init()
    {
        inventory = PlayerDataManager.Instance.GetPlayerInventory();
        if (inventory != null)
        {
            inventory.OnChanged += HandleRefresh;
            IEnumerable<PlayerCharacterData> data = inventory.GetAll();
            inventoryUI.Init(data);
        }
    }

    void HandleRefresh()
    {
        IEnumerable<PlayerCharacterData> data = inventory.GetAll();
        inventoryUI.Refresh(data);
    }

    
}
