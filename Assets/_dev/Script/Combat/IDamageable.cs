public interface IDamageable
{
    void TakeDamage(DamageInfo info);
    bool IsDead { get; }
}
