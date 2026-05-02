using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryUIManager uiManager;

    private PlayerInventory _inventory;

    private void OnEnable()
    {
        _inventory = PlayerDataManager.Instance?.GetInventory();
        if (_inventory != null)
        {
            _inventory.OnChanged -= HandleRefresh;
            _inventory.OnChanged += HandleRefresh;
        }

        if (uiManager != null)
        {
            uiManager.OnEquipItem          += HandleEquipItem;
            uiManager.OnUnequipItem        += HandleUnequipItem;
            uiManager.OnUseItem            += HandleUseItem;
            uiManager.OnDiscardItem        += HandleDiscardItem;
            uiManager.OnOpenTranscendence  += HandleOpenTranscendence;
            uiManager.OnEnhanceEquipment   += HandleEnhanceEquipment;
        }
    }

    private void OnDisable()
    {
        if (_inventory != null)
            _inventory.OnChanged -= HandleRefresh;

        if (uiManager != null)
        {
            uiManager.OnEquipItem         -= HandleEquipItem;
            uiManager.OnUnequipItem       -= HandleUnequipItem;
            uiManager.OnUseItem           -= HandleUseItem;
            uiManager.OnDiscardItem       -= HandleDiscardItem;
            uiManager.OnOpenTranscendence -= HandleOpenTranscendence;
            uiManager.OnEnhanceEquipment  -= HandleEnhanceEquipment;
        }
    }

    public void OpenInventory()
    {
        if (uiManager == null) return;
        uiManager.Open();
        RefreshUI();
    }

    private void HandleRefresh() { if (uiManager != null) RefreshUI(); }

    private void RefreshUI()
    {
        if (_inventory == null) { Debug.LogWarning("[InventoryController] _inventory null"); return; }
        if (uiManager == null)  { Debug.LogWarning("[InventoryController] uiManager null");  return; }

        var chars     = _inventory.GetAllCharacters().ToList();
        var items     = _inventory.GetAllItems().ToList();
        var equipment = _inventory.GetAllEquipment().ToList();

        Debug.Log($"[InventoryController] 갱신: 캐릭터 {chars.Count}, 아이템 {items.Count}, 장비 {equipment.Count}");
        uiManager.InitInventory(chars, items, equipment);
    }

    // ── 서버 응답 공통 처리 ───────────────────────────────────────────
    // 모든 API는 성공 시 { "success": true, "user": {...} } 반환
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

    // ── 장비 장착 ─────────────────────────────────────────────────────
    // 낙관적 업데이트 → 서버 전송 → 서버 응답으로 최종 동기화
    private async void HandleEquipItem(int characterId, int equipInstanceId, EquipmentSlotType slotType)
    {
        // 낙관적 업데이트
        var equipData = _inventory.GetEquipment(equipInstanceId);
        if (equipData != null)
            _inventory.EquipItem(characterId, equipData, slotType);

        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.EquipItem(userId, characterId, equipInstanceId, slotType);
            ApplyServerResponse(json, "EquipItem");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] EquipItem 오류: {e.Message}");
            _inventory.UnequipItem(characterId, slotType); // 롤백
        }
    }

    // ── 장비 해제 ─────────────────────────────────────────────────────
    private async void HandleUnequipItem(int characterId, EquipmentSlotType slotType)
    {
        _inventory.UnequipItem(characterId, slotType);

        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.UnequipItem(userId, characterId, slotType);
            ApplyServerResponse(json, "UnequipItem");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] UnequipItem 오류: {e.Message}");
        }
    }

    // ── 아이템 사용 ───────────────────────────────────────────────────
    private async void HandleUseItem(int characterId, int itemId)
    {
        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.UseItem(userId, itemId, characterId);
            ApplyServerResponse(json, "UseItem");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] UseItem 오류: {e.Message}");
        }
    }

    // ── 아이템 버리기 ─────────────────────────────────────────────────
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
            Debug.LogError($"[InventoryController] DiscardItem 오류: {e.Message}");
        }
    }

    // ── 캐릭터 초월 ───────────────────────────────────────────────────
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
            Debug.LogError($"[InventoryController] Transcend 오류: {e.Message}");
        }
    }

    // ── 장비 강화 ─────────────────────────────────────────────────────
    // 서버 응답: { success, enhanced, enhance, goldCost, successRate, user }
    private async void HandleEnhanceEquipment(int equipInstanceId)
    {
        try
        {
            int userId = PlayerDataManager.Instance.GetUserId();
            string json = await BackendManager.EnhanceEquipment(userId, equipInstanceId);

            var response = JObject.Parse(json);
            bool apiSuccess = response["success"] is JToken s && (bool)s;

            if (apiSuccess)
            {
                bool enhanced    = response["enhanced"] != null && (bool)response["enhanced"];
                int  newEnhance  = response["enhance"]  != null ? (int)response["enhance"] : 0;
                int  goldCost    = response["goldCost"] != null ? (int)response["goldCost"] : 0;

                Debug.Log($"[InventoryController] 강화 {(enhanced ? $"성공 (+{newEnhance})" : "실패")} — 골드 -{goldCost}");

                if (response["user"] is JToken user)
                    PlayerDataManager.Instance.ApplyUserData(user);
            }
            else
            {
                var msg = response["message"]?.ToString() ?? "알 수 없는 오류";
                Debug.LogWarning($"[InventoryController] EnhanceEquipment 서버 오류: {msg}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryController] EnhanceEquipment 오류: {e.Message}");
        }
    }
}
