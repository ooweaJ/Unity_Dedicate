using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class Backend
{
    private static readonly HttpClient client = new HttpClient();
    private static string baseUrl = "http://192.168.90.94:3000";

    public static async Task<string> Login(string username, string password)
    {
        var json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var res = await client.PostAsync(baseUrl + "/users/login", content);
        return await res.Content.ReadAsStringAsync();
    }

    public static async Task<string> GetCharacters(int userId)
    {
        var res = await client.GetAsync(baseUrl + $"/characters/{userId}");
        return await res.Content.ReadAsStringAsync();
    }
}
