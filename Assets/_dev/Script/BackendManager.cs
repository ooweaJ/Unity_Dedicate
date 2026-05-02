using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// =====================================================================
// [설계 원칙] 클라이언트는 "의도"만 서버에 전달합니다.
// 실제 효과(HP+100, ATK+50 등)는 서버 DB가 결정합니다.
//
// 모든 인벤토리/성장 API 응답 형식:
//   성공: { "success": true, "user": { ...전체 유저 데이터... } }
//   실패: { "success": false, "message": "오류 원인" }
//
// enhance/transcend는 추가 필드(enhanced, transcendStage 등)도 포함합니다.
// =====================================================================

public static class BackendManager
{
    private static readonly HttpClient client = new HttpClient();
    private static string baseUrl = "http://api.jaewoo98.store";

    // ─── 유저 ──────────────────────────────────────────────────────────

    // POST /users/login
    public static async Task<string> Login(string username, string password)
    {
        var json    = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + "/users/login", content);
        return await res.Content.ReadAsStringAsync();
    }

    // GET /users/:userId
    public static async Task<string> GetUserInfo(int userId)
    {
        var res = await client.GetAsync(baseUrl + $"/users/{userId}");
        return await res.Content.ReadAsStringAsync();
    }

    // POST /users/:userId/select-character
    // 캐릭터 선택 시 서버에 저장 → 재접속 시 복원
    public static async Task<string> SelectCharacter(int userId, int characterId)
    {
        var json    = $"{{\"characterId\":{characterId}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + $"/users/{userId}/select-character", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /users/:userId/battle-result
    // 전투 종료 후 획득 경험치 보고
    public static async Task<string> BattleResult(int userId, int gainedExp)
    {
        var json    = $"{{\"gainedExp\":{gainedExp}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + $"/users/{userId}/battle-result", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /users/:userId/transcend
    // 캐릭터 초월 — N초월에 조각 N개 소모, 100% 성공
    // 응답: { success, transcendStage, newMaxLevel, shardsUsed, user }
    public static async Task<string> TranscendCharacter(int userId, int characterId)
    {
        var json    = $"{{\"characterId\":{characterId}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + $"/users/{userId}/transcend", content);
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 매칭 ──────────────────────────────────────────────────────────

    // POST /match/acquire
    public static async Task<string> AcquirePort()
    {
        var res = await client.PostAsync(baseUrl + "/match/acquire", null);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /match/release
    public static async Task<string> ReleasePort(int port)
    {
        var json    = $"{{\"port\":{port}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + "/match/release", content);
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 가챠 ──────────────────────────────────────────────────────────

    // POST /gacha/draw/:userId
    public static async Task<string> GachaDraw(int userId, int bannerId, int amount)
    {
        var json    = $"{{\"bannerId\":{bannerId},\"amount\":{amount}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + $"/gacha/draw/{userId}", content);
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 인벤토리 ─────────────────────────────────────────────────────

    // POST /inventory/equip
    // equipInstanceId: user_items_equipment.id (강화 수치가 붙은 장비 인스턴스 ID)
    // slotType 값: "Weapon" | "Armor" | "Accessory" | "Ring"
    public static async Task<string> EquipItem(int userId, int characterId, int equipInstanceId, EquipmentSlotType slotType)
    {
        var json    = $"{{\"userId\":{userId},\"characterId\":{characterId},\"equipInstanceId\":{equipInstanceId},\"slotType\":\"{slotType}\"}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + "/inventory/equip", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/unequip
    public static async Task<string> UnequipItem(int userId, int characterId, EquipmentSlotType slotType)
    {
        var json    = $"{{\"userId\":{userId},\"characterId\":{characterId},\"slotType\":\"{slotType}\"}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + "/inventory/unequip", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/use  (exp_potion 등 소모품)
    public static async Task<string> UseItem(int userId, int itemId, int characterId)
    {
        var json    = $"{{\"userId\":{userId},\"itemId\":{itemId},\"characterId\":{characterId}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + "/inventory/use", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/discard
    public static async Task<string> DiscardItem(int userId, int itemId, int amount = 1)
    {
        var json    = $"{{\"userId\":{userId},\"itemId\":{itemId},\"amount\":{amount}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(baseUrl + "/inventory/discard", content);
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 장비 강화 ────────────────────────────────────────────────────

    // POST /equipment/:userId/enhance/:equipInstanceId
    // 골드 소모 + 확률 판정. 실패해도 골드만 소비 (강화 수치 유지)
    // 응답: { success, enhanced, enhance, goldCost, successRate, user }
    public static async Task<string> EnhanceEquipment(int userId, int equipInstanceId)
    {
        var res = await client.PostAsync(
            baseUrl + $"/equipment/{userId}/enhance/{equipInstanceId}", null);
        return await res.Content.ReadAsStringAsync();
    }
}
