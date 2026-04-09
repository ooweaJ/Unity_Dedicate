using UnityEngine;

[CreateAssetMenu(fileName = "GameDatabase", menuName = "Data/GameDatabase")]
public class GameDatabase : ScriptableObject
{
    public CharacterTableSO characterTable;
    public ItemTableSO itemTable;
}