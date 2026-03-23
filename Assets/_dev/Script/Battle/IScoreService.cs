using System.Threading.Tasks;

public interface IScoreService
{
    // 매치 결과를 백엔드에 기록
    // 서버에서만 호출
    Task ReportMatchResult(BattleResultData result);
}