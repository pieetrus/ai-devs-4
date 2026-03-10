using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _02_FindHim.Services;

internal class HubApiClient
{
    private static readonly HttpClient Http = new();
    private static readonly Uri LocationEndpoint = new("https://hub.ag3nts.org/api/location");
    private static readonly Uri AccessLevelEndpoint = new("https://hub.ag3nts.org/api/accesslevel");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly string _apiKey = Environment.GetEnvironmentVariable("AI_DEVS_API_KEY")
        ?? throw new InvalidOperationException("AI_DEVS_API_KEY not set");

    public async Task<List<Coordinate>> GetLocationsAsync(string name, string surname)
    {
        var payload = new { apikey = _apiKey, name, surname };
        var raw = await PostWithRetryAsync(LocationEndpoint, payload);
        return JsonSerializer.Deserialize<List<Coordinate>>(raw, JsonOpts) ?? [];
    }

    public async Task<int> GetAccessLevelAsync(string name, string surname, int birthYear)
    {
        var payload = new { apikey = _apiKey, name, surname, birthYear };
        var raw = await PostWithRetryAsync(AccessLevelEndpoint, payload);
        var result = JsonSerializer.Deserialize<AccessLevelResponse>(raw, JsonOpts);
        return result?.AccessLevel ?? 0;
    }

    private static async Task<string> PostWithRetryAsync<T>(Uri endpoint, T payload)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));

            var response = await Http.PostAsJsonAsync(endpoint, payload);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine($"  [429] rate limited, retrying in {attempt * 2 + 2}s...");
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2 + 2));
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        throw new HttpRequestException("Exceeded retry limit on 429 responses.");
    }
}

file record AccessLevelResponse(
    [property: JsonPropertyName("accessLevel")] int AccessLevel);
