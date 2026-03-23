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
///                   기본 SetActive(false)
///
/// Animator Layer 1 (Action Layer) 클립에서
/// AnimationEvent로 OnAttackStart() / OnAttackEnd() 호출
/// </summary>
public class HitBox : NetworkBehaviour
{
    // 한 스윙에 같은 대상 중복 히트 방지
    private readonly HashSet<Collider> alreadyHit = new HashSet<Collider>();

    private GameObject owner;
    private WeaponBase weapon;

    private void Awake()
    {
        owner = transform.root.gameObject; // Player 루트
        weapon = transform.root.GetComponent<WeaponBase>();

        // 시작 시 비활성화
        gameObject.SetActive(false);
    }

    // ─── AnimationEvent 에서 호출 ─────────────────────────────────────
    // Animator Controller → Layer 1 Attack 클립 → Add Event
    // Function: OnAttackStart (공격 판정 시작 프레임)
    public void OnAttackStart()
    {
        alreadyHit.Clear();
        gameObject.SetActive(true);
    }

    // Function: OnAttackEnd (공격 판정 종료 프레임)
    public void OnAttackEnd()
    {
        gameObject.SetActive(false);
    }

    // ─── 충돌 판정 (서버 전용) ────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        // 서버에서만 데미지 판정
        if (!isServer) return;

        // 자기 자신 제외
        if (other.transform.root.gameObject == owner) return;

        // 중복 히트 방지
        if (alreadyHit.Contains(other)) return;
        alreadyHit.Add(other);

        // IDamageable에 데미지 전달
        var damageable = other.transform.root.GetComponent<IDamageable>();
        if (damageable == null) return;

        float damage = weapon != null ? weapon.GetFinalDamage() : 20f;
        damageable.TakeDamage(damage, owner);

        // 피격 애니메이션
        other.transform.root
            .GetComponent<PlayerAnimationController>()
            ?.RpcPlayHit();
    }
}