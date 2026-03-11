using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] private BattleUI battleUI;

    void OnEnable()
    {
        BattleNetworkPlayer.OnPlayerJoined += HandlePlayerJoined;
    }

    void OnDisable()
    {
        BattleNetworkPlayer.OnPlayerJoined -= HandlePlayerJoined;
    }

    void HandlePlayerJoined(BattleNetworkPlayer player)
    {
        if (player.isLocalPlayer)
        {
            battleUI.SetMyInfo(player.nickname);

            // 나보다 먼저 들어온 상대방 체크
            foreach (var p in BattleNetworkPlayer.players)
            {
                if (!p.isLocalPlayer)
                    battleUI.SetEnemyInfo(p.nickname);
            }
        }
        else
        {
            battleUI.SetEnemyInfo(player.nickname);
        }
    }
}