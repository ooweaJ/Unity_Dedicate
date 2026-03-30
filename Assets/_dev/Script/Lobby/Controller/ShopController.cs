using UnityEngine;

public class ShopController : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private Camera lobbyCamera;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        if (shopUI != null)
        {
            shopUI.OnClikedGachaButton += OnClickGacha;
        }
    }
    void OnClickGacha()
    {
        if (lobbyCamera != null)
            lobbyCamera.gameObject.SetActive(false);
        if(shopUI != null)
            shopUI.gameObject.SetActive(false);
        GachaSceneLoader.Activate();
    }
}
