using System;
using Mirror;

/// <summary>
/// 캐릭터의 모든 최종 스탯을 담는 데이터 규격
/// 로비 인증, 서버 동기화, UI 표시 등에 공통으로 사용됩니다.
/// </summary>
[Serializable]
public struct CharacterStatData
{
    public float maxHp;
    public float atk;
    public float def;

    // 네트워크 전송을 위해 Mirror용 Read/Write 메서드가 필요할 수 있으나 
    // 기본 자료형들로만 구성되어 있어 Mirror가 자동으로 처리합니다.
}
