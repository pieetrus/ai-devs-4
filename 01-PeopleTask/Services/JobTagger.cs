using System.Text.Json;
using AiDevs.Shared;
using OpenAI.Chat;

namespace _01_PeopleTask.Services;

internal class JobTaggerAgent
{
    private const string Model = "openai/gpt-4o-mini";
    private const string SystemPrompt = "Assign one or more tags to each numbered job description. A person can have multiple tags.";

    private const string JsonSchema = """
                                      {
                                        "type": "object",
                                        "properties": {
                                          "results": {
                                            "type": "array",
                                            "items": {
                                              "type": "object",
                                              "properties": {
                                                "id":   { "type": "integer" },
                                                "tags": {
                                                  "type": "array",
                                                  "items": {
                                                    "type": "string",
                                                    "enum": ["IT","transport","edukacja","medycyna",
                                                             "praca z ludźmi","praca z pojazdami","praca fizyczna"]
                                                  }
                                                }
                                              },
                                              "required": ["id","tags"],
                                              "additionalProperties": false
                                            }
                                          }
                                        },
                                        "required": ["results"],
                                        "additionalProperties": false
                                      }
                                      """;

    private readonly ChatClient _chat = new LlmClient().GetChatClient(Model);

    public async Task<TaggingResult> TagJobsAsync(List<PersonRow> people)
    {
        var jobList = string.Join("\n", people.Select((p, i) => $"{i + 1}. {p.Job}"));

        var completion = await _chat.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage($"Ponumerowana lista stanowisk:\n{jobList}")
            ],
            new ChatCompletionOptions { ResponseFormat = BuildResponseFormat() });

        var rawJson = completion.Value.Content[0].Text;
        Console.WriteLine($"LLM response:\n{rawJson}\n");

        return JsonSerializer.Deserialize<TaggingResult>(rawJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static ChatResponseFormat BuildResponseFormat() =>
        ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "tagging_result",
            jsonSchema: BinaryData.FromString(JsonSchema),
            jsonSchemaIsStrict: true);
}