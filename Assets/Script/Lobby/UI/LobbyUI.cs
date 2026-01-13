using Mirror;
using System;
using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public event Action OnInventoryButtonClicked;
    public event Action OnStoreButtonClicked;
    public event Action OnMatchButtonClicked;

    public void InventoryButtonPressed()
    {
        OnInventoryButtonClicked?.Invoke();
    }

    public void StoreButtonPressed()
    {
        OnStoreButtonClicked?.Invoke();
    }

    public void OnClickMatchButtonPressed()
    {
        OnMatchButtonClicked?.Invoke();
    }
}
