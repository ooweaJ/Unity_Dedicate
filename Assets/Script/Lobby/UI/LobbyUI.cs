using System;
using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public event Action OnInventoryButtonClicked;
    public event Action OnStoreButtonClicked;

    public void InventoryButtonPressed()
    {
        OnInventoryButtonClicked?.Invoke();
    }

    public void StoreButtonPressed()
    {
        OnStoreButtonClicked?.Invoke();
    }
}
