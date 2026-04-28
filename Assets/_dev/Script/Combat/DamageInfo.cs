using UnityEngine;

public struct DamageInfo
{
    public float      Amount;
    public GameObject Attacker;
    public Vector3    Direction;
    public float      Knockback;

    public DamageInfo(float amount, GameObject attacker, Vector3 direction = default, float knockback = 0f)
    {
        Amount    = amount;
        Attacker  = attacker;
        Direction = direction;
        Knockback = knockback;
    }
}
