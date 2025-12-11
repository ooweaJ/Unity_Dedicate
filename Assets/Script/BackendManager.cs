using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class BackendManager
{
    private static readonly HttpClient client = new HttpClient();
    private static string baseUrl = "http://192.168.90.94:3000";

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

    // POST /characters/draw/:userId
    public static async Task<string> DrawCharacter(int userId)
    {
        var res = await client.PostAsync(baseUrl + $"/characters/draw/{userId}", null);
        return await res.Content.ReadAsStringAsync();
    }

    // POST /gacha/draw/:userId
    public static async Task<string> DrawGacha(int userId)
    {
        var res = await client.PostAsync(baseUrl + $"/gacha/draw/{userId}", null);
        return await res.Content.ReadAsStringAsync();
    }
}
