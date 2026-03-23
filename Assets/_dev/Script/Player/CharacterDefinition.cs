using UnityEngine;

/// <summary>
/// PF_SwordChan, PF_Mage 등 캐릭터 외형 프리팹에 붙이는 단순 데이터 홀더
/// NetworkBehaviour 아님 — SyncVar/Command 없음
/// "나는 이 SO의 데이터를 쓴다"고 선언하는 역할
/// </summary>
public class CharacterDefinition : MonoBehaviour
{
    [Tooltip("이 캐릭터 외형이 사용할 CharacterDataSO")]
    public CharacterDataSO data;
}
