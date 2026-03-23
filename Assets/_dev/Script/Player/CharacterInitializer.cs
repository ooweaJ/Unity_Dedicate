using Mirror;
using UnityEngine;

/// <summary>
/// Player 프리팹에 붙임
/// OnStartServer/OnStartClient에서 CharacterDataSO 기반으로 초기화
/// </summary>
public class CharacterInitializer : NetworkBehaviour
{
    [Tooltip("캐릭터마다 다른 CharacterDataSO 에셋 연결")]
    [SerializeField] private CharacterDataSO characterData;

    [Tooltip("무기 외형이 붙을 부모 오브젝트")]
    [SerializeField] private Transform weaponRoot;

    private CharacterWeapon  charWeapon;
    private CharacterStats   charStats;
    private PlayerController playerCtrl;

    private void Awake()
    {
        charWeapon = GetComponent<CharacterWeapon>();
        charStats  = GetComponent<CharacterStats>();
        playerCtrl = GetComponent<PlayerController>();
    }

    public override void OnStartServer()
    {
        if (characterData == null)
        {
            Debug.LogWarning("[INIT] CharacterDataSO가 없습니다!");
            return;
        }

        charStats?.SetData(characterData);
        charWeapon?.Setup(characterData.basicAttack, characterData.skillAttack);

        if (playerCtrl != null)
            playerCtrl.moveSpeed = characterData.moveSpeed;
    }

    public override void OnStartClient()
    {
        SetWeaponMesh();
    }

    private void SetWeaponMesh()
    {
        if (characterData?.weaponMeshPrefab == null || weaponRoot == null) return;

        foreach (Transform child in weaponRoot)
            Destroy(child.gameObject);

        Instantiate(characterData.weaponMeshPrefab, weaponRoot);
    }
}
