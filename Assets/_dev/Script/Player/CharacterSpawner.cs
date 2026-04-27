using Mirror;
using UnityEngine;

/// <summary>
/// Player.prefab에 붙이는 캐릭터 스폰 컴포넌트
///
/// 핵심 포인트:
/// - characterPrefabs 배열은 Player.prefab 인스펙터에서 직접 연결
///   (서버/클라이언트 둘 다 갖고 있어야 함)
/// - CustomNetworkManager에서 런타임으로 넣어주는 방식은 서버에서만 동작하므로 제거
///
/// 흐름:
/// [OnStartServer]
///   selectedCharacter enum → 프리팹 인덱스
///   CharacterDefinition에서 SO 읽음
///   CharacterStats.SetData(SO) + SetLevel(level)  ← SyncVar라 클라이언트로 자동 전파
///   CharacterWeapon.Setup(basic, skill)            ← 서버 판정용
///
/// [OnStartClient] — 모든 클라이언트 실행
///   characterTypeIndex(SyncVar)로 올바른 프리팹 선택
///   CharacterModelRoot에 외형 Instantiate
///   CharacterDefinition에서 SO 읽어 CharacterWeapon.Setup() 호출
///   → 클라이언트도 basicAttackData / skillAttackData 갖게 됨
///   → 공격키 입력 시 PlayAttackLocal() 정상 동작
/// </summary>
public class CharacterSpawner : NetworkBehaviour
{
    [Tooltip("CharacterType enum 순서대로 캐릭터 외형 프리팹 연결 — Player.prefab 인스펙터에서 직접 연결")]
    public GameObject[] characterPrefabs;

    [Tooltip("캐릭터 메시가 붙을 빈 오브젝트")]
    public Transform characterModelRoot;

    // SyncVar로 인덱스 동기화 → 클라이언트도 같은 프리팹 알 수 있음
    [SyncVar]
    private int characterTypeIndex = 0;

    private CharacterStats      charStats;
    private CharacterWeapon     charWeapon;
    private BattleNetworkPlayer battlePlayer;

    // 외부에서 Animator 참조가 필요할 때 구독
    public System.Action<Animator> OnCharacterSpawned;

    private void Awake()
    {
        charStats    = GetComponent<CharacterStats>();
        charWeapon   = GetComponent<CharacterWeapon>();
        battlePlayer = GetComponent<BattleNetworkPlayer>();
    }

    // ─── 서버: 스탯 + 무기 초기화 (SyncVar → 클라이언트 자동 전파) ──
    public override void OnStartServer()
    {
        if (battlePlayer == null)
        {
            Debug.LogError("[SPAWNER] BattleNetworkPlayer 없음");
            return;
        }

        int idx = (int)battlePlayer.selectedCharacter;

        if (!IsValidIndex(idx)) return;

        characterTypeIndex = idx; // SyncVar — 클라이언트로 전파

        var def = GetDefinition(idx);
        if (def == null) return;

        CharacterDataSO so = def.data;

        // CharacterStats는 SyncVar 포함 → 클라이언트 자동 반영
        charStats?.SetData(so);
        
        // 배틀 플레이어에 저장된 최종 스탯(인증 시 받은 값)을 적용
        if (battlePlayer != null)
        {
            charStats?.ApplyStats(battlePlayer.stats);
        }

        // 서버의 CharacterWeapon Setup (서버 판정용)
        charWeapon?.Setup(so.basicAttack, so.skillAttack);

        Debug.Log($"[SPAWNER] 서버 초기화: {so.characterName} | 최종ATK: {battlePlayer?.stats.atk}");
    }

    // ─── 클라이언트: 외형 + 무기 데이터 초기화 ───────────────────────
    public override void OnStartClient()
    {
        if (!IsValidIndex(characterTypeIndex)) return;

        // 1. 외형 붙이기
        SpawnVisual(characterTypeIndex);

        // 2. 클라이언트도 CharacterWeapon Setup 필요
        //    → 공격키 누르면 UseBasicAttack() → basicAttackData null 체크 통과
        //    → PlayAttackLocal() 정상 호출 → 애니메이션 재생
        var def = GetDefinition(characterTypeIndex);
        if (def != null)
            charWeapon?.Setup(def.data.basicAttack, def.data.skillAttack);
    }

    // ─── 내부 유틸 ────────────────────────────────────────────────────
    private bool IsValidIndex(int idx)
    {
        if (characterPrefabs == null || idx < 0 || idx >= characterPrefabs.Length)
        {
            Debug.LogError($"[SPAWNER] characterPrefabs[{idx}] 없음 — Player.prefab 인스펙터에서 배열 연결 확인");
            return false;
        }
        return true;
    }

    private CharacterDefinition GetDefinition(int idx)
    {
        var def = characterPrefabs[idx].GetComponent<CharacterDefinition>();
        if (def == null || def.data == null)
        {
            Debug.LogError($"[SPAWNER] {characterPrefabs[idx].name}에 CharacterDefinition 또는 data 없음");
            return null;
        }
        return def;
    }

    private void SpawnVisual(int idx)
    {
        if (characterModelRoot == null)
        {
            Debug.LogError("[SPAWNER] CharacterModelRoot 없음");
            return;
        }

        foreach (Transform child in characterModelRoot)
            Destroy(child.gameObject);

        GameObject visual = Instantiate(characterPrefabs[idx], characterModelRoot);

        Animator anim = visual.GetComponentInChildren<Animator>();
        if (anim != null)
            OnCharacterSpawned?.Invoke(anim);

        Debug.Log($"[SPAWNER] 외형 부착: {characterPrefabs[idx].name}");
    }
}
