using UnityEngine;

public class MasterDataManager : MonoBehaviour
{
    public static MasterDataManager Instance;

    public CharacterDatabase characterDB;

    void Awake()
    {
        Instance = this;
    }
}
