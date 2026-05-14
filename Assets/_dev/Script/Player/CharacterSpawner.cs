using Mirror;
using UnityEngine;

public class CharacterSpawner : NetworkBehaviour
{
    [Tooltip("캐릭터 메시가 붙을 빈 오브젝트")]
    public Transform characterModelRoot;

    // BattleNetworkPlayer.selectedCharacter(SyncVar)를 직접 사용하므로 별도 SyncVar 불필요

    private CharacterStats      charStats;
    private CharacterWeapon     charWeapon;
    private BattleNetworkPlayer battlePlayer;

    public System.Action<Animator> OnCharacterSpawned;

    private void Awake()
    {
        charStats    = GetComponent<CharacterStats>();
        charWeapon   = GetComponent<CharacterWeapon>();
        battlePlayer = GetComponent<BattleNetworkPlayer>();
    }

    // ─── 서버: 스탯 + 무기 초기화 ────────────────────────────────────────
    public override void OnStartServer()
    {
        if (battlePlayer == null)
        {
            Debug.LogError("[SPAWNER] BattleNetworkPlayer 없음");
            return;
        }

        Debug.Log($"[SPAWNER-DIAG] selectedCharacter={battlePlayer.selectedCharacter}");

        if (GameDataManager.Instance == null)
        {
            Debug.LogError("[SPAWNER-DIAG] GameDataManager.Instance가 NULL — 서버가 부트씬을 안 탔을 가능성");
            return;
        }
        if (GameDataManager.Instance.GetType().GetField("database",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(GameDataManager.Instance) == null)
        {
            Debug.LogError("[SPAWNER-DIAG] GameDataManager.database가 NULL — ScriptableObject 미연결");
        }

        var rawData = GameDataManager.Instance.GetCharacterByType(battlePlayer.selectedCharacter);
        Debug.Log($"[SPAWNER-DIAG] GetCharacterByType({battlePlayer.selectedCharacter}) → {(rawData == null ? "NULL" : rawData.displayName)} (id={rawData?.id}, type={rawData?.type})");

        if (rawData == null)
        {
            Debug.LogError($"[SPAWNER] CharacterTable에 {battlePlayer.selectedCharacter} 없음");
            return;
        }

        if (rawData.battleData == null)
        {
            Debug.LogError($"[SPAWNER] {rawData.displayName}의 battleData(CharacterDataSO) 미연결");
            return;
        }

        charStats?.SetData(rawData.battleData);
        charStats?.ApplyStats(battlePlayer.stats);
        charWeapon?.Setup(rawData.battleData.skills);

        Debug.Log($"[SPAWNER] 서버 초기화: {rawData.displayName} | ATK: {battlePlayer.stats.atk}");
    }

    // ─── 클라이언트: 외형 + 무기 데이터 초기화 ───────────────────────────
    public override void OnStartClient()
    {
        Debug.Log($"[SPAWNER-DIAG] OnStartClient — selectedCharacter={battlePlayer.selectedCharacter}, GameDataManager={GameDataManager.Instance != null}");

        var rawData = GameDataManager.Instance?.GetCharacterByType(battlePlayer.selectedCharacter);
        Debug.Log($"[SPAWNER-DIAG] 클라이언트 GetCharacterByType({battlePlayer.selectedCharacter}) → {(rawData == null ? "NULL" : rawData.displayName)}");

        if (rawData == null) return;

        SpawnVisual(rawData.modelPrefab);

        if (rawData.battleData != null)
            charWeapon?.Setup(rawData.battleData.skills);
    }

    public CharacterDataSO GetCharacterData()
    {
        return GameDataManager.Instance
            ?.GetCharacterByType(battlePlayer.selectedCharacter)
            ?.battleData;
    }

    // ─── 내부 유틸 ────────────────────────────────────────────────────────
    private void SpawnVisual(GameObject prefab)
    {
        if (characterModelRoot == null)
        {
            Debug.LogError("[SPAWNER] CharacterModelRoot 없음");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError("[SPAWNER] modelPrefab이 CharacterTable에 연결되지 않음");
            return;
        }

        foreach (Transform child in characterModelRoot)
            Destroy(child.gameObject);

        GameObject visual = Instantiate(prefab, characterModelRoot);

        var anim = visual.GetComponentInChildren<Animator>();
        if (anim != null) OnCharacterSpawned?.Invoke(anim);

        GetComponent<PlayerBushState>()?.CacheRenderers(characterModelRoot);

        Debug.Log($"[SPAWNER] 외형 부착: {prefab.name}");
    }

    public void HideVisual()
    {
        if (characterModelRoot != null)
            characterModelRoot.gameObject.SetActive(false);
    }
}
