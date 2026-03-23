using Mirror;
using UnityEngine;

public class MyAuthenticator : NetworkAuthenticator
{
    public struct AuthRequestMessage : NetworkMessage
    {
        public int userId;
        public string nickname;
        public int level;
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
        Debug.Log($"[SERVER] AuthRequest received: {msg.nickname}");

        conn.authenticationData = msg;

        ServerAccept(conn);

        conn.Send(new AuthResponseMessage { success = true });
    }


    // --- 클라이언트 측 로직 ---
    public override void OnStartClient()
    {
        // 클라이언트도 서버의 응답을 기다릴 준비를 합니다.
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
    }
    private void OnAuthResponseMessage(AuthResponseMessage msg)
    {
        if (msg.success)
        {
            Debug.Log("[CLIENT] Auth Success! Calling ClientAccept.");
            ClientAccept();
        }
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn)
    {
        Debug.Log("[SERVER] Waiting for auth message...");
    }


    public override void OnClientAuthenticate()
    {
        AuthRequestMessage msg = new AuthRequestMessage
        {
            userId = PlayerDataManager.Instance.GetUserId(),
            nickname = PlayerDataManager.Instance.GetUsername(),
            level = PlayerDataManager.Instance.GetLevel()
        };

        Debug.Log("[CLIENT] Sending AuthRequest");

        NetworkClient.Send(msg);
    }
}