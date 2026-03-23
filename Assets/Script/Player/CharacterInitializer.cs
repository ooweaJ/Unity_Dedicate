using Mirror;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// 플레이어 프리팹에 붙임
/// OnStartServer에서 CharacterDataSO 기반으로 무기/스탯 초기화
/// </summary>
public class CharacterInitializer : NetworkBehaviour
{
    [SerializeField] private CharacterDataSO characterData;
    [SerializeField] private Transform weaponRoot;   // WeaponRoot 오브젝트

    private PlayerCombat combat;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
    }

    public override void OnStartServer()
    {
        if (characterData == null) return;
        InitWeapons();
    }

    public override void OnStartClient()
    {
        // 클라이언트에서 무기 메시 세팅 (비주얼)
        SetWeaponMesh(characterData?.primaryWeapon);
    }

    [Server]
    private void InitWeapons()
    {
        // primaryWeapon SO 기반으로 MeleeWeapon 또는 ProjectileWeapon 동적 추가
        var primaryWeapon = AddWeaponComponent(characterData.primaryWeapon);
        var skillWeapon = AddWeaponComponent(characterData.skillWeapon);

        combat?.EquipPrimary(primaryWeapon);
        combat?.EquipSkill(skillWeapon);
    }

    private WeaponBase AddWeaponComponent(WeaponDataSO data)
    {
        if (data == null) return null;

        // 기존 같은 타입 제거
        WeaponBase existing = data.attackType == AttackType.Melee
            ? (WeaponBase)GetComponent<MeleeWeapon>()
            : GetComponent<ProjectileWeapon>();
        if (existing != null) Destroy(existing);

        // SO의 attackType에 따라 컴포넌트 동적 추가
        WeaponBase weapon = data.attackType == AttackType.Melee
            ? (WeaponBase)gameObject.AddComponent<MeleeWeapon>()
            : gameObject.AddComponent<ProjectileWeapon>();

        return weapon;
    }

    // 무기 메시를 WeaponRoot에 붙임 (비주얼)
    private void SetWeaponMesh(WeaponDataSO data)
    {
        if (data?.weaponMeshPrefab == null || weaponRoot == null) return;

        foreach (Transform child in weaponRoot)
            Destroy(child.gameObject);

        Instantiate(data.weaponMeshPrefab, weaponRoot);
    }
}
