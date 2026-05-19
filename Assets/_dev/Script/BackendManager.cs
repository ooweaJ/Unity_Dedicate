using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        var content = ToJson(new { username, password });
        var res     = await client.PostAsync(baseUrl + "/users/login", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /users/register
    public static async Task<string> Register(string username, string password)
    {
        var content = ToJson(new { username, password });
        var res     = await client.PostAsync(baseUrl + "/users/register", content);
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
        var res = await client.PostAsync(baseUrl + $"/users/{userId}/select-character", ToJson(new { characterId }));
        return await res.Content.ReadAsStringAsync();
    }

    // POST /users/:userId/battle-result
    // 전투 종료 후 획득 경험치 보고
    public static async Task<string> BattleResult(int userId, int gainedExp)
    {
        var res = await client.PostAsync(baseUrl + $"/users/{userId}/battle-result", ToJson(new { gainedExp }));
        return await res.Content.ReadAsStringAsync();
    }

    // POST /users/:userId/transcend
    // 캐릭터 초월 — shardsToUse / shardsRequired 확률 판정, 실패해도 조각 소모
    // 응답: { success, transcendSuccess, transcendStage, newMaxLevel, shardsUsed, user }
    public static async Task<string> TranscendCharacter(int userId, int characterId, int shardsToUse)
    {
        var res = await client.PostAsync(baseUrl + $"/users/{userId}/transcend", ToJson(new { characterId, shardsToUse }));
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
        var res = await client.PostAsync(baseUrl + "/match/release", ToJson(new { port }));
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 가챠 ──────────────────────────────────────────────────────────

    // POST /gacha/draw/:userId
    public static async Task<string> GachaDraw(int userId, int bannerId, int amount)
    {
        var res = await client.PostAsync(baseUrl + $"/gacha/draw/{userId}", ToJson(new { bannerId, amount }));
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 인벤토리 ─────────────────────────────────────────────────────

    // POST /inventory/equip
    // equipInstanceId: user_items_equipment.id (강화 수치가 붙은 장비 인스턴스 ID)
    // slotType 값: "Weapon" | "Armor" | "Accessory" | "Ring"
    public static async Task<string> EquipItem(int userId, int characterId, int equipInstanceId, EquipmentSlotType slotType)
    {
        var res = await client.PostAsync(baseUrl + "/inventory/equip", ToJson(new { userId, characterId, equipInstanceId, slotType = slotType.ToString() }));
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/unequip
    public static async Task<string> UnequipItem(int userId, int characterId, EquipmentSlotType slotType)
    {
        var res = await client.PostAsync(baseUrl + "/inventory/unequip", ToJson(new { userId, characterId, slotType = slotType.ToString() }));
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/use  (exp_potion 등 소모품)
    public static async Task<string> UseItem(int userId, int itemId, int characterId)
    {
        var res = await client.PostAsync(baseUrl + "/inventory/use", ToJson(new { userId, itemId, characterId }));
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/discard
    public static async Task<string> DiscardItem(int userId, int itemId, int amount = 1)
    {
        var res = await client.PostAsync(baseUrl + "/inventory/discard", ToJson(new { userId, itemId, amount }));
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 장비 강화 ────────────────────────────────────────────────────

    // GET /equipment/:userId/enhance/:equipInstanceId/preview
    // 골드 소모 없이 비용·확률만 조회
    // 응답: { success, maxEnhance, currentEnhance, successRate, goldCost }
    public static async Task<string> GetEnhancePreview(int userId, int equipInstanceId)
    {
        var res = await client.GetAsync(
            baseUrl + $"/equipment/{userId}/enhance/{equipInstanceId}/preview");
        return await res.Content.ReadAsStringAsync();
    }

    // POST /equipment/:userId/enhance/:equipInstanceId
    // 골드 소모 + 확률 판정. 실패해도 골드만 소비 (강화 수치 유지)
    // 응답: { success, enhanced, enhance, goldCost, successRate, user }
    public static async Task<string> EnhanceEquipment(int userId, int equipInstanceId)
    {
        var res = await client.PostAsync(
            baseUrl + $"/equipment/{userId}/enhance/{equipInstanceId}", null);
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 공통 ──────────────────────────────────────────────────────────

    private static StringContent ToJson(object payload)
    {
        var json = JsonConvert.SerializeObject(payload);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
