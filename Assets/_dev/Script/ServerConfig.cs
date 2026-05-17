public static class ServerConfig
{
    // 로컬 WiFi 테스트 시 PC의 로컬 IP로 변경 (ipconfig로 확인)
    // 예: "192.168.0.10"
    // 배포 시: 실제 서버 공인 IP로 교체
    public const string GameServerIP = "192.168.3.11";

    public const ushort LobbyPort  = 7777;
    public const ushort BattlePortBase = 7778;
}
