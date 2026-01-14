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

    public void Awake()
    {
        // 서버 시작 시 한 번만 등록
        if (Application.isBatchMode)
        {
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage);
        }
    }

    private void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        Debug.Log($"[SERVER] AuthRequest received: {msg.nickname}");
        conn.authenticationData = msg;
        ServerAccept(conn); // 플레이어 스폰 허용
    }



    // 클라이언트에서 호출됨
    public override void OnClientAuthenticate()
    {
        // 로그인 직후 여기서 메시지 보내면 됨
        int userId = PlayerDataManager.Instance.GetUserId();
        string nickname = PlayerDataManager.Instance.GetUsername();
        int level = PlayerDataManager.Instance.GetLevel();

        AuthRequestMessage msg = new AuthRequestMessage
        {
            userId = userId,
            nickname = nickname,
            level = level
        };

        NetworkClient.Send(msg); // 서버로 전송
    }
}
