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
            // 여기서 ClientAccept를 호출해야 NetworkManager가 OnServerAddPlayer를 실행합니다.
            ClientAccept();

            if (!NetworkClient.ready)
                NetworkClient.Ready();

            NetworkClient.AddPlayer();
        }
    }
    public override void OnServerAuthenticate(NetworkConnectionToClient conn)
    {
        Debug.Log("[SERVER] Waiting for auth message...");
    }

    private void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        Debug.Log($"[SERVER] AuthRequest received: {msg.nickname}");

        conn.authenticationData = msg;

        ServerAccept(conn);

        conn.Send(new AuthResponseMessage { success = true });
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