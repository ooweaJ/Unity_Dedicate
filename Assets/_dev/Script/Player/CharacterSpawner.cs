using Mirror;
using UnityEngine;

/// <summary>
/// Player.prefab에 붙이는 캐릭터 스폰 컴포넌트
///
/// 흐름:
/// 1. OnStartServer()
///    → BattleNetworkPlayer.selectedCharacter 읽음
///    → characterPrefabs[enum] 에서 캐릭터 외형 프리팹 선택
///    → 프리팹의 CharacterDefinition에서 CharacterDataSO 가져옴
///    → CharacterStats.SetData(SO), CharacterWeapon.Setup(SO의 공격 데이터)
///    → CharacterStats.SetLevel(level) → 강화 수치 반영
///
/// 2. OnStartClient() — 모든 클라이언트
///    → CharacterModelRoot에 캐릭터 외형 프리팹 Instantiate
///    → 화면에 올바른 캐릭터 메시 표시
///
/// CustomNetworkManager.characterPrefabs 배열의 순서 =
/// CharacterType enum 순서와 반드시 일치
/// </summary>
public class CharacterSpawner : NetworkBehaviour
{
    [Tooltip("CharacterType enum 순서대로 캐릭터 외형 프리팹 연결")]
    public GameObject[] characterPrefabs;

    [Tooltip("캐릭터 메시가 붙을 빈 오브젝트")]
    public Transform characterModelRoot;

    public System.Action<Animator> OnCharacterSpawned; // 캐릭터 스폰시 애니메이터 캐싱

    // SyncVar로 캐릭터 타입 동기화 — 클라이언트에서 올바른 메시 생성에 사용
    [SyncVar]
    private int characterTypeIndex = 0;

    private CharacterStats  charStats;
    private CharacterWeapon charWeapon;
    private BattleNetworkPlayer battlePlayer;

    private void Awake()
    {
        charStats   = GetComponent<CharacterStats>();
        charWeapon  = GetComponent<CharacterWeapon>();
        battlePlayer = GetComponent<BattleNetworkPlayer>();
    }

    // ─── 서버: 스탯 + 무기 초기화 ────────────────────────────────────
    public override void OnStartServer()
    {
        if (battlePlayer == null)
        {
            Debug.LogError("[SPAWNER] BattleNetworkPlayer 없음");
            return;
        }

        int idx = (int)battlePlayer.selectedCharacter;

        if (characterPrefabs == null || idx >= characterPrefabs.Length)
        {
            Debug.LogError($"[SPAWNER] characterPrefabs[{idx}] 없음! 배열 확인 필요");
            return;
        }

        // SyncVar에 인덱스 저장 → 클라이언트에서 같은 프리팹 Instantiate에 사용
        characterTypeIndex = idx;

        // 프리팹에서 CharacterDefinition으로 SO 가져옴
        var def = characterPrefabs[idx].GetComponent<CharacterDefinition>();
        if (def == null || def.data == null)
        {
            Debug.LogError($"[SPAWNER] {characterPrefabs[idx].name}에 CharacterDefinition 또는 data 없음");
            return;
        }

        CharacterDataSO so = def.data;

        // 서버 컴포넌트에 데이터 주입
        charStats?.SetData(so);
        charStats?.SetLevel(battlePlayer.level);  // 로비 레벨 → 강화 수치
        charWeapon?.Setup(so.basicAttack, so.skillAttack);

        Debug.Log($"[SPAWNER] {so.characterName} 초기화 완료 | 레벨: {battlePlayer.level}");
    }

    // ─── 클라이언트: 캐릭터 외형 붙이기 ─────────────────────────────
    public override void OnStartClient()
    {
        if (characterModelRoot == null)
        {
            Debug.LogError("[SPAWNER] CharacterModelRoot 없음!");
            return;
        }

        if (characterPrefabs == null || characterTypeIndex >= characterPrefabs.Length)
        {
            Debug.LogError($"[SPAWNER] characterPrefabs[{characterTypeIndex}] 없음");
            return;
        }

        // 기존 메시 제거 (재연결 등 대비)
        foreach (Transform child in characterModelRoot)
            Destroy(child.gameObject);

        // 캐릭터 외형 프리팹을 CharacterModelRoot 자식으로 생성
        GameObject visualObj = Instantiate(characterPrefabs[characterTypeIndex], characterModelRoot);
        Animator childAnimator = visualObj.GetComponentInChildren<Animator>();

        if (childAnimator != null)
        {
            OnCharacterSpawned?.Invoke(childAnimator);

            Debug.Log($"[SPAWNER] 캐릭터 메시 부착: {characterPrefabs[characterTypeIndex].name}");
        }
    }
}
