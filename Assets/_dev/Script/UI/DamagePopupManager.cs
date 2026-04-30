using UnityEngine;

/// <summary>
/// 데미지 팝업 스폰을 담당하는 싱글턴 매니저.
/// 씬에 배치 후 DamagePopup 프리팹을 인스펙터에서 연결한다.
/// </summary>
public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private DamagePopup prefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Show(float amount, Vector3 worldPos)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[DamagePopup] 프리팹 미연결");
            return;
        }

        var popup = Instantiate(prefab, worldPos, Quaternion.identity);
        popup.Setup(amount);
    }
}
