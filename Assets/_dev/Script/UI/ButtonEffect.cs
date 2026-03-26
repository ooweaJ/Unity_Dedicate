using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Material targetMaterial;

    // 쉐이더에서 설정한 파라미터 이름 (기본값: _IsHovered)
    [SerializeField] private string parameterName = "_Hovered";

    void Awake()
    {
        // Image 컴포넌트에서 머티리얼을 가져옵니다.
        // 중요: 공유 머티리얼을 쓰면 모든 버튼이 같이 변하므로, 
        // 개별 버튼마다 독립적으로 작동하게 하기 위해 인스턴스화합니다.
        Image img = GetComponent<Image>();
        if (img != null)
        {
            targetMaterial = img.material;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Test");
        if (targetMaterial != null)
            targetMaterial.SetFloat(parameterName, 1.0f); // 호버 시 1
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetMaterial != null)
            targetMaterial.SetFloat(parameterName, 0.0f); // 평상시 0
    }
}