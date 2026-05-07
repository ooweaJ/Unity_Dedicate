using UnityEngine;

public enum ShaderFadeType
{
    Auto,          // URP Lit / Standard 자동 감지
    PropertyValue, // 커스텀 셰이더 — 프로퍼티 이름·값 직접 지정 (Toon 등)
}

[System.Serializable]
public struct FloatPropertyOverride
{
    public string name;
    public float  value;
}

/// <summary>
/// 셰이더별 반투명 처리 방법 정의.
/// Project 우클릭 → Create → Game/ShaderFadeProfile
///
/// Auto:          URP Lit(_Surface)·Standard(_Mode) 자동 감지
/// PropertyValue: 프로퍼티 직접 지정 + 블렌드 모드 강제 설정 (UTS Toon 등)
///                additionalProperties로 필요한 프로퍼티 추가 가능
/// </summary>
[CreateAssetMenu(fileName = "NewShaderFadeProfile", menuName = "Game/ShaderFadeProfile")]
public class ShaderFadeProfile : ScriptableObject
{
    public ShaderFadeType type = ShaderFadeType.Auto;

    [Header("Auto 전용")]
    [Range(0f, 1f)]
    public float alpha = 0.4f;

    [Header("PropertyValue 전용")]
    [Tooltip("투명도를 제어하는 셰이더 프로퍼티 이름 (예: _Tweak_transparency)")]
    public string propertyName = "_Tweak_transparency";
    [Tooltip("부쉬 안 투명 상태의 값")]
    public float  fadeValue    = -0.4f;

    [Tooltip("함께 설정할 추가 프로퍼티 (선택)")]
    public FloatPropertyOverride[] additionalProperties;
}
