using UnityEngine;

/// <summary>
/// 클라이언트 전용 이펙트 스폰 창구
/// ClientRpc에서 EffectType + position만 받아서 로컬 Instantiate
/// → NetworkManager 등록 불필요
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [SerializeField] private EffectDatabase database;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Play(EffectType type, Vector3 position, Quaternion rotation = default)
    {
        if (type == EffectType.None) return;

        var entry = database?.Get(type);
        if (entry?.prefab == null)
        {
            Debug.LogWarning($"[EFFECT] {type} 프리팹 미등록 — EffectDatabase 확인");
            return;
        }

        var fx = Instantiate(entry.prefab, position, rotation == default ? Quaternion.identity : rotation);
        Destroy(fx, entry.lifeTime);
    }
}
