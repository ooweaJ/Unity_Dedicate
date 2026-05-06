using UnityEngine;

public struct DamageInfo
{
    public float         Amount;
    public GameObject    Attacker;
    public Vector3       Direction;
    public StatusEffect  Effect;

    public DamageInfo(float amount, GameObject attacker, Vector3 direction = default, StatusEffect effect = default)
    {
        Amount    = amount;
        Attacker  = attacker;
        Direction = direction;
        Effect    = effect;
    }
}
