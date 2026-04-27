using Mirror;
using UnityEngine;
using System.Linq; // 필수 추가

public class MyAuthenticator : NetworkAuthenticator
{
    public struct AuthRequestMessage : NetworkMessage
    {
        public int           userId;
        public string        nickname;
        public int           level;
        public CharacterType selectedCharacter;
        public CharacterStatData stats;
    }

    public struct AuthResponseMessage : NetworkMessage
    {
        public bool success;
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
    }

    private void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        Debug.Log($"[SERVER] AuthRequest: {msg.nickname} | ATK: {msg.stats.atk}");
        conn.authenticationData = msg;
        ServerAccept(conn);
        conn.Send(new AuthResponseMessage { success = true });
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
    }

    private void OnAuthResponseMessage(AuthResponseMessage msg)
    {
        if (msg.success)
        {
            Debug.Log("[CLIENT] Auth 성공");
            ClientAccept();
        }
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn) { }

    public override void OnClientAuthenticate()
    {
        var inventory = PlayerDataManager.Instance.GetInventory();
        var selectedType = PlayerDataManager.Instance.GetSelectedCharacter();
        
        // Find -> FirstOrDefault로 수정
        var ownedChar = inventory.GetAllCharacters()
            .FirstOrDefault(c => {
                var staticData = GameDataManager.Instance.GetCharacter(c.characterId);
                return staticData != null && staticData.type == selectedType;
            });

        CharacterStatData finalStats = new CharacterStatData();

        if (ownedChar != null)
        {
            var staticData = GameDataManager.Instance.GetCharacter(ownedChar.characterId);
            finalStats.atk = staticData.baseAtk * (1f + (ownedChar.level - 1) * 0.1f);
            finalStats.def = staticData.baseDef * (1f + (ownedChar.level - 1) * 0.05f);
            finalStats.maxHp = staticData.baseHp * (1f + (ownedChar.level - 1) * 0.1f);

            foreach (var itemId in ownedChar.equippedItems.Values)
            {
                var item = GameDataManager.Instance.GetItem(itemId);
                if (item != null)
                {
                    finalStats.atk += item.atkBonus;
                    finalStats.def += item.defBonus;
                }
            }
        }

        var msg = new AuthRequestMessage
        {
            userId            = PlayerDataManager.Instance.GetUserId(),
            nickname          = PlayerDataManager.Instance.GetUsername(),
            level             = PlayerDataManager.Instance.GetLevel(),
            selectedCharacter = selectedType,
            stats             = finalStats
        };

        NetworkClient.Send(msg);
    }
}
