using Mirror;
using UnityEngine;

public abstract class WeaponBase : NetworkBehaviour
{
    // SO에서 데이터 주입 — 인스펙터 직접 입력 대신 SO 참조
    [SerializeField] protected WeaponDataSO weaponData;

    protected CharacterStats stats;

    // WeaponDataSO 프로퍼티 단축
    protected float Cooldown => weaponData != null ? weaponData.attackCooldown : 0.8f;
    protected float Range => weaponData != null ? weaponData.attackRange : 2.5f;
    protected LayerMask Layer => weaponData != null ? weaponData.targetLayer : 0;

    private float lastAttackTime = -99f;

    protected virtual void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    public void Attack(Vector3 origin, Vector3 direction)
    {
        if (!CanAttack()) return;
        lastAttackTime = Time.time;
        OnAttack(origin, direction);
    }

    public bool CanAttack() => Time.time - lastAttackTime >= Cooldown;

    // public으로 열어둠 — HitBox에서 참조
    public float GetFinalDamage()
    {
        float multiplier = weaponData != null ? weaponData.damageMultiplier : 1f;
        if (stats != null) return stats.FinalAttack * multiplier;
        return 20f * multiplier;
    }

    // SO의 무기 메시를 WeaponRoot에 붙이는 초기화
    public void InitWeaponMesh(Transform weaponRoot)
    {
        if (weaponData?.weaponMeshPrefab == null || weaponRoot == null) return;

        // 기존 메시 제거
        foreach (Transform child in weaponRoot)
            Destroy(child.gameObject);

        Instantiate(weaponData.weaponMeshPrefab, weaponRoot);
    }

    protected abstract void OnAttack(Vector3 origin, Vector3 direction);
}