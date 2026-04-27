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
            var s = StatUtils.Calculate(staticData, ownedChar.level, ownedChar.equippedItems.Values);
            finalStats.atk   = s.atk;
            finalStats.def   = s.def;
            finalStats.maxHp = s.hp;
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
