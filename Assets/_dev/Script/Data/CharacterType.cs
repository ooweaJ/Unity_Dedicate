/// <summary>
/// 캐릭터 종류 enum
/// 인덱스 순서 = CustomNetworkManager.characterDataArray 배열 순서와 반드시 일치
/// 새 캐릭터 추가 시 맨 뒤에 추가 (중간 삽입 금지 — 기존 인덱스 깨짐)
/// </summary>
public enum CharacterType
{
    chan = 1,
    sapphi = 2,
    // 추가 캐릭터는 여기에
}
