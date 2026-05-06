public enum StatusEffectType { None, Knockback, Stun, Slow, DotDamage }

[System.Serializable]
public struct StatusEffect
{
    public StatusEffectType type;
    [UnityEngine.Tooltip("Knockback=힘(N)  Slow=감속배율(0~1)  DotDamage=틱당 데미지  Stun=미사용")]
    public float value;
    [UnityEngine.Tooltip("Stun/Slow/DotDamage 지속 시간 (Knockback은 무시)")]
    public float duration;
    [UnityEngine.Tooltip("DotDamage 전용: 틱 간격(초)")]
    public float interval;

    public bool IsNone => type == StatusEffectType.None;
    public static StatusEffect None => default;
}
