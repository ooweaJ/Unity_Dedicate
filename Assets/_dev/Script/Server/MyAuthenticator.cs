using Mirror;
using UnityEngine;

public class MyAuthenticator : NetworkAuthenticator
{
    public struct AuthRequestMessage : NetworkMessage
    {
        public int           userId;
        public string        nickname;
        public int           level;
        public CharacterType selectedCharacter;  // 로비에서 고른 캐릭터
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
        Debug.Log($"[SERVER] AuthRequest: {msg.nickname} | 캐릭터: {msg.selectedCharacter}");
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

    public override void OnServerAuthenticate(NetworkConnectionToClient conn)
    {
        Debug.Log("[SERVER] Auth 메시지 대기 중...");
    }

    public override void OnClientAuthenticate()
    {
        var msg = new AuthRequestMessage
        {
            userId            = PlayerDataManager.Instance.GetUserId(),
            nickname          = PlayerDataManager.Instance.GetUsername(),
            level             = PlayerDataManager.Instance.GetLevel(),
            selectedCharacter = PlayerDataManager.Instance.GetSelectedCharacter()
        };

        Debug.Log($"[CLIENT] Auth 전송: {msg.nickname} | 캐릭터: {msg.selectedCharacter}");
        NetworkClient.Send(msg);
    }
}
