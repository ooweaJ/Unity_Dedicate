using UnityEngine;
using Newtonsoft.Json.Linq;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryUIManager uiManager;
    [SerializeField] private LobbyUI lobbyUI;

    private PlayerInventory _inventory;

    private void OnEnable()
    {
        if (PlayerDataManager.Instance == null) return;

        if (lobbyUI != null)
            lobbyUI.OnInventoryButtonClicked += OpenInventory;

        _inventory = PlayerDataManager.Instance.GetInventory();
        if (_inventory != null)
        {
            _inventory.OnChanged -= HandleRefresh;
            _inventory.OnChanged += HandleRefresh;
        }

        if (uiManager != null)
        {
            uiManager.OnEquipItem         += HandleEquipItem;
            uiManager.OnUnequipItem       += HandleUnequipItem;
            uiManager.OnUseItem           += HandleUseItem;
            uiManager.OnDiscardItem       += HandleDiscardItem;
            uiManager.OnOpenTranscendence += HandleOpenTranscendence;
        }
    }

    private void OnDisable()
    {
        if (_inventory != null)
            _inventory.OnChanged -= HandleRefresh;

        if (lobbyUI != null)
            lobbyUI.OnInventoryButtonClicked -= OpenInventory;

        if (uiManager != null)
        {
            uiManager.OnEquipItem         -= HandleEquipItem;
            uiManager.OnUnequipItem       -= HandleUnequipItem;
            uiManager.OnUseItem           -= HandleUseItem;
            uiManager.OnDiscardItem       -= HandleDiscardItem;
            uiManager.OnOpenTranscendence -= HandleOpenTranscendence;
        }
    }

    public void OpenInventory()
    {
        if (uiManager == null) return;
        uiManager.Open();
        RefreshUI();
    }

    private void HandleRefresh()
    {
        if (uiManager != null)
            RefreshUI();
    }

    private void RefreshUI()
    {
        if (_inventory == null || uiManager == null) return;
        uiManager.InitInventory(_inventory.GetAllCharacters(), _inventory.GetAllItems());
    }

    // ─── 서버 응답 공통 처리 ──────────────────────────────────────────
    // 모든 인벤토리 API는 성공 시 { "success": true, "user": {...} } 를 반환합니다.
    // user 객체를 ApplyUserData에 넘기면 전체 인벤토리가 서버 기준으로 동기화됩니다.
    private void ApplyServerResponse(string rawJson, string action)
    {
        if (string.IsNullOrEmpty(rawJson)) return;

        var response = JObject.Parse(rawJson);
        if (response["success"] is JToken s && (bool)s)
        {
            if (response["user"] is JToken user)
                PlayerDataManager.Instance.ApplyUserData(user);
        }
        else
        {
            var msg = response["message"]?.ToString() ?? "알 수 없는 오류";
            Debug.LogWarning($"[InventoryController] {action} 서버 오류: {msg}");
        }
    }

    // ─── 장비 장착 ────────────────────────────────────────────────────
    // 흐름: 로컬 즉시 반영(낙관적 업데이트) → 서버 전송 → 서버 응답으로 최종 동기화
    // 낙관적 업데이트: 서버 응답 기다리지 않고 UI를 먼저 바꿔 반응성 확보
    private async void HandleEquipItem(int characterId, int itemId, EquipmentSlotType slotType)
    {
        // 1. 로컬 즉시 반영
        _inventory.EquipItem(characterId, itemId, slotType);

        // 2. 서버 전송 후 서버 기준으로 최종 동기화
        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.EquipItem(userId, characterId, itemId, slotType);
            ApplyServerResponse(json, "EquipItem");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] EquipItem 통신 오류: {e.Message}");
            // TODO: 서버 실패 시 로컬 롤백 → _inventory.UnequipItem(characterId, slotType)
        }
    }

    // ─── 장비 해제 ────────────────────────────────────────────────────
    private async void HandleUnequipItem(int characterId, EquipmentSlotType slotType)
    {
        // 1. 로컬 즉시 반영
        _inventory.UnequipItem(characterId, slotType);

        // 2. 서버 전송
        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.UnequipItem(userId, characterId, slotType);
            ApplyServerResponse(json, "UnequipItem");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] UnequipItem 통신 오류: {e.Message}");
        }
    }

    // ─── 아이템 사용 ──────────────────────────────────────────────────
    // 사용 효과(HP 회복 등)는 서버가 DB에서 결정합니다.
    // 클라이언트는 itemId만 전달 → 서버가 effect_type, effect_value를 조회해 처리
    private async void HandleUseItem(int itemId)
    {
        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.UseItem(userId, itemId);
            ApplyServerResponse(json, "UseItem");
            // ApplyUserData → OnChanged → UI 자동 갱신 (아이템 수량 감소 반영)
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] UseItem 통신 오류: {e.Message}");
        }
    }

    // ─── 아이템 버리기 ────────────────────────────────────────────────
    private async void HandleDiscardItem(int itemId)
    {
        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.DiscardItem(userId, itemId);
            ApplyServerResponse(json, "DiscardItem");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] DiscardItem 통신 오류: {e.Message}");
        }
    }

    // ─── 초월 ────────────────────────────────────────────────────────
    // characterId: 초월할 캐릭터 ID (조각 소모 대상도 같은 캐릭터)
    private async void HandleOpenTranscendence(int characterId)
    {
        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.TranscendCharacter(userId, characterId);
            ApplyServerResponse(json, "Transcend");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] Transcend 통신 오류: {e.Message}");
        }
    }
}
