using System.Text.Json;
using AiDevs.Shared;
using OpenAI.Chat;

namespace _05_Railway.Services;

internal class RailwayAgent(LlmClient llm, RailwayApiClient api)
{
    private readonly ChatClient _chat = llm.GetChatClient("anthropic/claude-haiku-4-5");
    private string? _flag;

    public async Task<string> RunAsync()
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("""
                You are activating railway route X-01 via a hub API.
                The API accepts { answer: <value> } where the value you send determines what happens.

                Start by calling the API with answer="help" to read the documentation.
                Then follow the documented steps exactly to activate route X-01.
                Stop as soon as the response contains {FLG:...} and report the flag.
                """),
            new UserChatMessage("Activate railway route X-01. Call the API with answer=\"help\" first.")
        };

        var options = new ChatCompletionOptions();
        options.Tools.Add(BuildCallApiTool());

        for (var iteration = 1; iteration <= 20 && _flag is null; iteration++)
        {
            Console.WriteLine($"\n--- Agent iteration {iteration} ---");
            var response = (await _chat.CompleteChatAsync(messages, options)).Value;
            messages.Add(new AssistantChatMessage(response));

            if (response.FinishReason == ChatFinishReason.Stop) break;
            if (response.FinishReason != ChatFinishReason.ToolCalls) continue;

            foreach (var toolCall in response.ToolCalls)
            {
                Console.WriteLine($"  Tool call: {toolCall.FunctionName}({toolCall.FunctionArguments})");
                var result = await DispatchToolAsync(toolCall);
                messages.Add(new ToolChatMessage(toolCall.Id, result));
                if (_flag is not null) return _flag;
            }
        }

        return _flag ?? "No flag found.";
    }

    private async Task<string> DispatchToolAsync(ChatToolCall toolCall)
    {
        using var doc = JsonDocument.Parse(toolCall.FunctionArguments);
        var args = doc.RootElement;

        // The LLM provides "answer" as a JSON value (string, number, or object)
        var answerEl = args.GetProperty("answer");
        object answer = answerEl.ValueKind switch
        {
            JsonValueKind.String => answerEl.GetString()!,
            JsonValueKind.Number => (object)answerEl.GetDouble(),
            _ => JsonSerializer.Deserialize<object>(answerEl.GetRawText())!
        };

        var body = await api.CallAsync(answer);

        if (body.Contains("{FLG:"))
        {
            var start = body.IndexOf("{FLG:");
            var end = body.IndexOf('}', start) + 1;
            _flag = body[start..end];
            Console.WriteLine($"\n=== FLAG DETECTED: {_flag} ===");
        }

        return body;
    }

    private static ChatTool BuildCallApiTool() =>
        ChatTool.CreateFunctionTool(
            "call_api",
            "Call the railway hub API. Pass the value for the 'answer' field.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "answer": {
                      "description": "The value to send as the 'answer' field. Can be a string (e.g. 'help') or an object with action-specific fields."
                    }
                  },
                  "required": ["answer"],
                  "additionalProperties": false
                }
                """));
}
