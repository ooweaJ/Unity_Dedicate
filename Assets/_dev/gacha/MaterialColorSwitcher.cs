using UnityEngine;
using UnityEngine.InputSystem;

public class MaterialColorSwitcher : MonoBehaviour
{
    private Renderer targetRenderer;
    private Material targetMaterial;

    // 제시해주신 HDR 컬러 값 설정
    // Color(R, G, B, A) 순서입니다.
    private Color defaultColor = new Color(5.42476034f, 4.45487833f, 2.40531754f, 1f);
    private Color pinkColor = new Color(5.42476082f, 2.41416049f, 4.1633358f, 1f);

    void Start()
    {
        // 오브젝트의 Renderer와 Material을 가져옵니다.
        targetRenderer = GetComponent<Renderer>();

        // .material을 호출하면 해당 오브젝트 전용 복제본 머티리얼이 생성되어 
        // 원본 프로젝트 에셋이 변하는 것을 방지합니다.
        targetMaterial = targetRenderer.material;
    }

    void Update()
    {
        // 신형 Input System 방식: Keyboard.current 사용
        var keyboard = Keyboard.current;
        if (keyboard == null) return; // 키보드가 연결 안 된 경우 예외 처리

        // 숫자 1키를 눌렀을 때 (wasPressedThisFrame은 GetKeyDown과 동일)
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            SetMaterialColor(defaultColor);
        }

        // 숫자 2키를 눌렀을 때
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            SetMaterialColor(pinkColor);
        }
    }

    private void SetMaterialColor(Color newColor)
    {
        // 셰이더의 메인 컬러 속성 이름이 "_BaseColor"(URP) 또는 "_Color"(Standard)인지 확인 필요
        // 보통 URP 셰이더 그래프라면 "_BaseColor"를 많이 사용합니다.
        if (targetMaterial.HasProperty("_BaseColor"))
        {
            targetMaterial.SetColor("_BaseColor", newColor);
        }
        else
        {
            targetMaterial.SetColor("_Color", newColor);
        }
    }
}