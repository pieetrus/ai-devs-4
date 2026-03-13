using System.Net.Http.Json;
using System.Text.Json;

namespace _05_Railway.Services;

internal class RailwayApiClient
{
    private static readonly HttpClient Http = new();
    private static readonly Uri VerifyEndpoint = new("https://hub.ag3nts.org/verify");
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _apiKey = Environment.GetEnvironmentVariable("AI_DEVS_API_KEY")
        ?? throw new InvalidOperationException("AI_DEVS_API_KEY not set");

    /// <summary>Posts { apikey, task: "railway", answer } to the verify endpoint.</summary>
    public async Task<string> CallAsync(object answer)
    {
        var payload = new { apikey = _apiKey, task = "railway", answer };

        var answerJson = JsonSerializer.Serialize(answer);
        Console.WriteLine($"  [API] answer={answerJson[..Math.Min(200, answerJson.Length)]}");

        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (attempt > 0)
            {
                var backoff = (int)Math.Pow(2, attempt - 1);
                Console.WriteLine($"  [retry #{attempt}] waiting {backoff}s...");
                await Task.Delay(TimeSpan.FromSeconds(backoff));
            }

            var response = await Http.PostAsJsonAsync(VerifyEndpoint, payload, JsonOpts);
            Console.WriteLine($"  [API] status={response.StatusCode}");

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Console.WriteLine("  [503] service unavailable, retrying...");
                continue;
            }

            await HandleRateLimitHeadersAsync(response);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine("  [429] rate limited, retrying...");
                continue;
            }

            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  [API] response={body[..Math.Min(300, body.Length)]}");
            return body;
        }

        throw new HttpRequestException("Exceeded retry limit.");
    }

    private static async Task HandleRateLimitHeadersAsync(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) &&
            int.TryParse(remaining.FirstOrDefault(), out var rem) && rem == 0)
        {
            if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetVals) &&
                long.TryParse(resetVals.FirstOrDefault(), out var resetEpoch))
            {
                var delay = DateTimeOffset.FromUnixTimeSeconds(resetEpoch) - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    Console.WriteLine($"  [rate-limit] sleeping {delay.TotalSeconds:F0}s until reset...");
                    await Task.Delay(delay);
                }
            }
        }

        if (response.Headers.TryGetValues("Retry-After", out var retryAfter) &&
            int.TryParse(retryAfter.FirstOrDefault(), out var seconds))
        {
            Console.WriteLine($"  [rate-limit] Retry-After {seconds}s, sleeping...");
            await Task.Delay(TimeSpan.FromSeconds(seconds));
        }
    }
}
