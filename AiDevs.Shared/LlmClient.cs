using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace AiDevs.Shared;

public class LlmClient
{
    private static readonly Uri OpenRouterEndpoint = new("https://openrouter.ai/api/v1");

    private readonly OpenAIClient _client;

    public LlmClient(string? apiKey = null)
    {
        var key = apiKey
            ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? throw new InvalidOperationException("OPENROUTER_API_KEY not set");

        _client = new OpenAIClient(
            new ApiKeyCredential(key),
            new OpenAIClientOptions { Endpoint = OpenRouterEndpoint });
    }

    public ChatClient GetChatClient(string model = "openai/gpt-4o-mini") =>
        _client.GetChatClient(model);
}
