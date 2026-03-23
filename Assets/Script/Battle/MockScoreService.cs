using System.Threading.Tasks;
using UnityEngine;

public class MockScoreService : IScoreService
{
    public async Task ReportMatchResult(BattleResultData result)
    {
        // 개발 중: 실제 API 대신 로그만 출력
        Debug.Log($"[SCORE] 매치 결과 기록 — 승자: {result.winnerName}");
        foreach (var p in result.playerResults)
            Debug.Log($"  {p.playerName} | 승:{p.isWinner} | 킬:{p.kills} | 데미지:{p.damageDealt:F0}");

        // 실제 API 호출 시 await HttpClient.PostAsync(...) 형태로 교체
        await Task.CompletedTask;
    }
}