using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// =====================================================================
// [설계 원칙] 왜 아이템 ID만 서버에 보내는가?
//
// 클라이언트는 "어떤 행동을 했다"는 의도만 서버에 전달합니다.
// 실제 효과(HP+100, ATK+50 등)는 서버 DB가 결정합니다.
//
// 나쁜 예: client.Send("HP를 100 올려줘") → 패킷 조작으로 9999 올리기 가능
// 좋은 예: client.Send("아이템 201번 사용") → 서버가 DB에서 201번 효과 조회·적용
//
// 모든 엔드포인트 응답 형식:
// {
//   "success": true,
//   "user": { ...전체 유저 데이터 (PlayerData.Apply() 그대로 넘기면 됨)... }
// }
// 실패 시: { "success": false, "message": "오류 원인" }
// =====================================================================

public static class BackendManager
{
    private static readonly HttpClient client = new HttpClient();
    private static string baseUrl = "http://api.jaewoo98.store";

    // ─── 기존 API ─────────────────────────────────────────────────────

    // POST /users/login
    public static async Task<string> Login(string username, string password)
    {
        var json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage res = await client.PostAsync(baseUrl + "/users/login", content);
        return await res.Content.ReadAsStringAsync();
    }

    // GET /characters/:userId
    public static async Task<string> GetCharacters(int userId)
    {
        var res = await client.GetAsync(baseUrl + $"/characters/{userId}");
        return await res.Content.ReadAsStringAsync();
    }

    // GET /users/:userId
    public static async Task<string> GetUserInfo(int userId)
    {
        var res = await client.GetAsync(baseUrl + $"/users/{userId}");
        return await res.Content.ReadAsStringAsync();
    }

    // POST /match/acquire
    public static async Task<string> AcquirePort()
    {
        HttpResponseMessage res = await client.PostAsync(baseUrl + "/match/acquire", null);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /match/release
    public static async Task<string> ReleasePort(int port)
    {
        var json = $"{{\"port\":{port}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage res = await client.PostAsync(baseUrl + "/match/release", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /gacha/draw/:userId
    public static async Task<string> GachaDraw(int userId, int bannerId, int amount)
    {
        var json = $"{{\"bannerId\":{bannerId},\"amount\":{amount}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await client.PostAsync(baseUrl + $"/gacha/draw/{userId}", content);
        return await res.Content.ReadAsStringAsync();
    }

    // ─── 인벤토리 API ─────────────────────────────────────────────────

    // POST /inventory/equip
    // 서버 처리 순서:
    //   1. userId + token 검증
    //   2. 해당 유저가 itemId 아이템을 보유하는지 DB 확인
    //   3. 같은 캐릭터·슬롯에 기존 장착 아이템 있으면 인벤토리로 반환
    //   4. equipped_items 테이블에 (characterId, slotType, itemId) upsert
    //   5. items 테이블에서 itemId 수량 1 감소 (0이면 레코드 삭제)
    //   6. 변경된 전체 user 데이터 반환
    // slotType 값: "Weapon" | "Armor" | "Accessory" | "Ring"
    public static async Task<string> EquipItem(int userId, int characterId, int itemId, EquipmentSlotType slotType)
    {
        var json = $"{{\"userId\":{userId},\"characterId\":{characterId},\"itemId\":{itemId},\"slotType\":\"{slotType}\"}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await client.PostAsync(baseUrl + "/inventory/equip", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/unequip
    // 서버 처리 순서:
    //   1. userId + token 검증
    //   2. equipped_items 테이블에서 (characterId, slotType) 레코드 조회 → itemId 획득
    //   3. 해당 레코드 삭제
    //   4. items 테이블에 itemId 수량 1 증가 (없으면 insert)
    //   5. 변경된 전체 user 데이터 반환
    public static async Task<string> UnequipItem(int userId, int characterId, EquipmentSlotType slotType)
    {
        var json = $"{{\"userId\":{userId},\"characterId\":{characterId},\"slotType\":\"{slotType}\"}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await client.PostAsync(baseUrl + "/inventory/unequip", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/use
    // 서버 처리 순서:
    //   1. userId + token 검증
    //   2. 해당 유저가 itemId를 1개 이상 보유하는지 확인
    //   3. items_table에서 itemId의 effect_type, effect_value 조회
    //      예) { effect_type: "heal_hp", effect_value: 100 }
    //   4. effect_type별 분기 처리 (heal_hp → character HP 증가, buff → 버프 DB 기록 등)
    //   5. items 테이블에서 해당 유저의 itemId 수량 1 감소
    //   6. 변경된 전체 user 데이터 반환
    // 아이템 ID만 넘기는 이유: 효과는 서버 DB가 결정, 클라이언트가 효과를 주장하면 조작 위험
    public static async Task<string> UseItem(int userId, int itemId)
    {
        var json = $"{{\"userId\":{userId},\"itemId\":{itemId}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await client.PostAsync(baseUrl + "/inventory/use", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/discard
    // 서버 처리 순서:
    //   1. userId + token 검증
    //   2. 해당 유저가 itemId를 amount 개 이상 보유하는지 확인
    //   3. items 테이블에서 수량 amount 감소 (0이면 레코드 삭제)
    //   4. 변경된 전체 user 데이터 반환
    public static async Task<string> DiscardItem(int userId, int itemId, int amount = 1)
    {
        var json = $"{{\"userId\":{userId},\"itemId\":{itemId},\"amount\":{amount}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await client.PostAsync(baseUrl + "/inventory/discard", content);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /inventory/transcend
    // 서버 처리 순서:
    //   1. userId + token 검증
    //   2. characters 테이블에서 해당 캐릭터의 shard_amount 확인
    //   3. 초월에 필요한 조각 수 충족 여부 확인 (character_table에서 transcend_cost 조회)
    //   4. shard_amount 차감, enhance +1 (또는 단계별 처리)
    //   5. 변경된 전체 user 데이터 반환
    public static async Task<string> TranscendCharacter(int userId, int characterId)
    {
        var json = $"{{\"userId\":{userId},\"characterId\":{characterId}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await client.PostAsync(baseUrl + "/inventory/transcend", content);
        return await res.Content.ReadAsStringAsync();
    }
}
