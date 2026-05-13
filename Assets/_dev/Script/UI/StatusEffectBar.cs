using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    [Header("Colors")]
    [SerializeField] private Color colorStun      = new Color(1f,   0.8f, 0f);
    [SerializeField] private Color colorSlow      = new Color(0.3f, 0.7f, 1f);
    [SerializeField] private Color colorDotDamage = new Color(0.8f, 0.2f, 1f);

    private Coroutine  _routine;
    private PlayerHPBar _hpBar;

    private void Awake()
    {
        _hpBar = GetComponent<PlayerHPBar>();

        if (fillImage != null)
        {
            fillImage.type       = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.gameObject.SetActive(false);
        }
    }

    public void Show(StatusEffectType type, float duration)
    {
        if (duration <= 0f || fillImage == null) return;

        fillImage.color = type switch
        {
            StatusEffectType.Stun      => colorStun,
            StatusEffectType.Slow      => colorSlow,
            StatusEffectType.DotDamage => colorDotDamage,
            _                          => Color.white
        };

        string label = type switch
        {
            StatusEffectType.Stun      => "기절",
            StatusEffectType.Slow      => "슬로우",
            StatusEffectType.DotDamage => "중독",
            _                          => ""
        };
        if (!string.IsNullOrEmpty(label))
            _hpBar?.ShowStatusText(label);

        if (_routine != null) StopCoroutine(_routine);
        fillImage.gameObject.SetActive(true);
        _routine = StartCoroutine(Deplete(duration));
    }

    public void Hide()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        if (fillImage != null) fillImage.gameObject.SetActive(false);
        _hpBar?.RestoreNickname();
    }

    private IEnumerator Deplete(float duration)
    {
        fillImage.fillAmount = 1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed             += Time.deltaTime;
            fillImage.fillAmount = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        fillImage.gameObject.SetActive(false);
        _hpBar?.RestoreNickname();
        _routine = null;
    }
}
