using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] private BattleUI battleUI;

    private string enemyName;

    void OnEnable()
    {
        LobbyNetworkPlayer.OnPlayerReady += HandlePlayer;
    }

    void OnDisable()
    {
        LobbyNetworkPlayer.OnPlayerReady -= HandlePlayer;
    }

    void HandlePlayer(LobbyNetworkPlayer player)
    {
        if (!player.isLocalPlayer)
        {
            enemyName = player.nickname;
            battleUI.UpdateUI(enemyName);
        }
    }
}
