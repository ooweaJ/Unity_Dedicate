using System.Collections.Generic;
using UnityEngine;
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public int userId;
    public string username;
    public int level;
    public int gold;

    public List<Character> characters = new List<Character>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

[System.Serializable]
public class Character
{
    public int id;
    public string name;
    public int level;
    public int hp;
    public int atk;

}
