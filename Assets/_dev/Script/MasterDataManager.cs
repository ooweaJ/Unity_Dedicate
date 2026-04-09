using UnityEngine;

public class MasterDataManager : MonoBehaviour
{
    public static MasterDataManager Instance;

    public InventoryDataBase characterDB;

    void Awake()
    {
        Instance = this;
    }
}
