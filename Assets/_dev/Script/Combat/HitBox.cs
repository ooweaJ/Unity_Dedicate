using Mirror;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 무기 오브젝트에 붙이는 히트박스 — 다단히트 근접 공격 전용
/// PerformMelee(OverlapSphere)로 충분한 경우에는 사용하지 않아도 됨
///
/// 프리팹 구조:
/// Player
/// └── WeaponRoot
///     └── HitBox  ← 이 스크립트 + CapsuleCollider (isTrigger=true)
///
/// Animator Layer 1 (Action Layer) Attack 클립에서
/// AnimationEvent → OnAttackStart() / OnAttackEnd() 호출
/// </summary>
public class HitBox : NetworkBehaviour
{
    private readonly HashSet<Collider> alreadyHit = new HashSet<Collider>();

    private GameObject     owner;
    private CharacterWeapon weapon;

    private void Awake()
    {
        owner  = transform.root.gameObject;
        weapon = transform.root.GetComponent<CharacterWeapon>();
        gameObject.SetActive(false);
    }

    public void OnAttackStart()
    {
        alreadyHit.Clear();
        gameObject.SetActive(true);
    }

    public void OnAttackEnd()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (other.transform.root.gameObject == owner) return;
        if (alreadyHit.Contains(other)) return;

        alreadyHit.Add(other);

        float dmg  = weapon != null ? weapon.GetFinalDamage() : 20f;
        var   info = new DamageInfo(dmg, owner, owner.transform.forward);

        other.transform.root.GetComponent<IDamageable>()?.TakeDamage(info);
        other.transform.root.GetComponent<PlayerAnimationController>()
            ?.RpcPlayHit(other.transform.position, EffectType.MeleeHit);
    }
}
