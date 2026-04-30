using TMPro;
using UnityEngine;

/// <summary>
/// 피격 시 월드 공간에 뜨는 데미지 숫자 팝업.
/// DamagePopupManager.Show()로 생성한다.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;
    [SerializeField] private float       floatSpeed = 2.5f;
    [SerializeField] private float       lifetime   = 0.9f;

    private float _elapsed;
    private Color _baseColor;

    public void Setup(float amount)
    {
        label.text = Mathf.RoundToInt(amount).ToString();
        _baseColor = label.color;
        _elapsed   = 0f;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // 카메라를 향하게
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        float alpha = Mathf.Lerp(1f, 0f, _elapsed / lifetime);
        label.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);

        if (_elapsed >= lifetime)
            Destroy(gameObject);
    }
}
