using UnityEngine;

/// <summary>
/// 캐릭터 비주얼 프리팹에 붙이는 설정 컴포넌트.
/// PlayerBushState.CacheRenderers가 이 값을 읽어 셰이더별 투명 처리를 결정한다.
/// </summary>
public class CharacterVisualConfig : MonoBehaviour
{
    [Tooltip("null이면 URP/Standard 자동 감지. Toon 등 커스텀 셰이더는 프로필 연결 필요.")]
    public ShaderFadeProfile fadeProfile;
}
