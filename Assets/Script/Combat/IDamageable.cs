/// <summary>
/// 데미지를 받을 수 있는 모든 오브젝트의 공통 인터페이스
/// 언리얼 UDamageType 시스템 대응
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage, UnityEngine.GameObject attacker);
    bool IsDead { get; }
}
