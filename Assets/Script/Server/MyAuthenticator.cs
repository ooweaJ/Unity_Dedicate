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
        // ���� ���� �� �� ���� ���
        if (Application.isBatchMode)
        {
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage);
        }
    }

    private void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        Debug.Log($"[SERVER] AuthRequest received: {msg.nickname}");
        conn.authenticationData = msg;
        ServerAccept(conn); // �÷��̾� ���� ���
    }



    // Ŭ���̾�Ʈ���� ȣ���
    public override void OnClientAuthenticate()
    {
        // �α��� ���� ���⼭ �޽��� ������ ��
        int userId = PlayerDataManager.Instance.GetUserId();
        string nickname = PlayerDataManager.Instance.GetUsername();
        int level = PlayerDataManager.Instance.GetLevel();

        AuthRequestMessage msg = new AuthRequestMessage
        {
            userId = userId,
            nickname = nickname,
            level = level
        };

        NetworkClient.Send(msg); // ������ ����
    }
}
