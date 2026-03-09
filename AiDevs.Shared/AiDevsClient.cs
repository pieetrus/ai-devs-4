using System.Net.Http.Json;
using System.Text.Json;

namespace AiDevs.Shared;

public class AiDevsClient(string? apiKey = null)
{
    private static readonly HttpClient Http = new();
    private static readonly Uri VerifyEndpoint = new("https://hub.ag3nts.org/verify");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _apiKey = apiKey
                                      ?? Environment.GetEnvironmentVariable("AI_DEVS_API_KEY")
                                      ?? throw new InvalidOperationException("AI_DEVS_API_KEY not set");

    public async Task<string> VerifyAsync<T>(string task, T answer)
    {
        var payload = new { apikey = _apiKey, task, answer };
        var response = await Http.PostAsJsonAsync(VerifyEndpoint, payload, JsonOptions);
        return await response.Content.ReadAsStringAsync();
    }
}
