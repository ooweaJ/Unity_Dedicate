using System;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button GachaButton;

    public event Action<int> OnClikedGachaButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Open()
    {
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    public void OnClickGachaButton(int amount)
    {
        OnClikedGachaButton.Invoke(amount);
    }
}
