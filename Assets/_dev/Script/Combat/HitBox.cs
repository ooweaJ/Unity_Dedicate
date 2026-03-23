using Mirror;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 무기 오브젝트에 붙이는 히트박스
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

    // AnimationEvent: 칼이 앞으로 나오는 프레임
    public void OnAttackStart()
    {
        alreadyHit.Clear();
        gameObject.SetActive(true);
    }

    // AnimationEvent: 칼이 돌아오는 프레임
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

        float damage = weapon != null ? weapon.GetFinalDamage() : 20f;

        other.transform.root.GetComponent<IDamageable>()
            ?.TakeDamage(damage, owner);

        other.transform.root.GetComponent<PlayerAnimationController>()
            ?.RpcPlayHit();
    }
}
