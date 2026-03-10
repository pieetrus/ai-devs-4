using System.Text.Json;
using AiDevs.Shared;
using OpenAI.Chat;

namespace _02_FindHim.Services;

internal class FindHimAgent(AiDevsClient aiDevs, LlmClient llm, HubApiClient hub)
{
    private readonly ChatClient _chat = llm.GetChatClient("openai/gpt-4o-mini");
    private bool _done;

    public async Task RunAsync(List<Suspect> suspects, List<PowerPlant> plants)
    {
        var suspectList = string.Join("\n", suspects.Select(s => $"- {s.Name} {s.Surname} (born {s.BirthYear})"));
        var plantList = string.Join("\n", plants.Select(p => $"- {p.Code}: {p.CityName} at ({p.Lat}, {p.Lon})"));

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage($"""
                You are an investigator. Your task is to find which suspect was spotted near a nuclear power plant.

                Suspects:
                {suspectList}

                Nuclear power plants:
                {plantList}

                Instructions:
                1. For each suspect, call get_person_locations to retrieve their known locations.
                2. Each location includes a closestPlant with distanceKm — the distance to the nearest power plant.
                3. For each suspect, find their minimum distanceKm across ALL their locations.
                4. The suspect with the SMALLEST minimum distanceKm is the one spotted near a plant.
                5. Call get_access_level for ALL suspects to get their access levels.
                6. Call submit_answer with the suspect who has the smallest minimum distance to any plant.
                7. If the server rejects the answer, reconsider — try the next closest suspect.
                """),
            new UserChatMessage("Begin the investigation. Check all suspects and find who was near a nuclear power plant.")
        };

        var options = new ChatCompletionOptions();
        options.Tools.Add(BuildGetPersonLocationsTool());
        options.Tools.Add(BuildGetAccessLevelTool());
        options.Tools.Add(BuildSubmitAnswerTool());

        var iteration = 0;
        while (iteration++ < 15 && !_done)
        {
            Console.WriteLine($"\n--- Agent iteration {iteration} ---");
            var response = (await _chat.CompleteChatAsync(messages, options)).Value;
            messages.Add(new AssistantChatMessage(response));

            if (response.FinishReason == ChatFinishReason.Stop) break;
            if (response.FinishReason != ChatFinishReason.ToolCalls) continue;

            foreach (var toolCall in response.ToolCalls)
            {
                Console.WriteLine($"  Tool call: {toolCall.FunctionName}({toolCall.FunctionArguments})");
                var result = await DispatchToolAsync(toolCall, plants);
                messages.Add(new ToolChatMessage(toolCall.Id, result));
                if (_done) return;
            }
        }
    }

    private async Task<string> DispatchToolAsync(ChatToolCall toolCall, List<PowerPlant> plants)
    {
        using var doc = JsonDocument.Parse(toolCall.FunctionArguments);
        var args = doc.RootElement;

        return toolCall.FunctionName switch
        {
            "get_person_locations" => await HandleGetLocations(args, plants),
            "get_access_level"     => await HandleGetAccessLevel(args),
            "submit_answer"        => await HandleSubmitAnswer(args),
            _                      => $"{{\"error\": \"unknown tool {toolCall.FunctionName}\"}}"
        };
    }

    private async Task<string> HandleGetLocations(JsonElement args, List<PowerPlant> plants)
    {
        var name    = args.GetProperty("name").GetString()!;
        var surname = args.GetProperty("surname").GetString()!;

        var locations = (await hub.GetLocationsAsync(name, surname)).Select(coord =>
        {
            var (plant, dist) = plants
                .Select(p => (plant: p, dist: HaversineKm(coord.Lat, coord.Lon, p.Lat, p.Lon)))
                .MinBy(x => x.dist);

            return new
            {
                lat = coord.Lat,
                lon = coord.Lon,
                closestPlant = new { plant.Code, plant.CityName, distanceKm = Math.Round(dist, 2) }
            };
        });

        return JsonSerializer.Serialize(new { locations });
    }

    private async Task<string> HandleGetAccessLevel(JsonElement args)
    {
        var accessLevel = await hub.GetAccessLevelAsync(
            args.GetProperty("name").GetString()!,
            args.GetProperty("surname").GetString()!,
            args.GetProperty("birth_year").GetInt32());

        return JsonSerializer.Serialize(new { accessLevel });
    }

    private async Task<string> HandleSubmitAnswer(JsonElement args)
    {
        var name        = args.GetProperty("name").GetString()!;
        var surname     = args.GetProperty("surname").GetString()!;
        var accessLevel = args.GetProperty("access_level").GetInt32();
        var powerPlant  = args.GetProperty("power_plant").GetString()!;

        var verifyResponse = await aiDevs.VerifyAsync("findhim", new { name, surname, accessLevel, powerPlant });
        Console.WriteLine($"\n=== SUBMIT: {name} {surname}, level {accessLevel}, plant {powerPlant} ===");
        Console.WriteLine($"Response: {verifyResponse}");

        if (verifyResponse.Contains("\"code\":0") || verifyResponse.Contains("FLG:"))
            _done = true;

        return verifyResponse;
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static ChatTool BuildGetPersonLocationsTool() =>
        ChatTool.CreateFunctionTool(
            "get_person_locations",
            "Get known locations for a person and compute distance to nearest nuclear power plant.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "name":    { "type": "string", "description": "Person's first name" },
                    "surname": { "type": "string", "description": "Person's surname" }
                  },
                  "required": ["name", "surname"],
                  "additionalProperties": false
                }
                """));

    private static ChatTool BuildGetAccessLevelTool() =>
        ChatTool.CreateFunctionTool(
            "get_access_level",
            "Get the security access level for a person.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "name":       { "type": "string",  "description": "Person's first name" },
                    "surname":    { "type": "string",  "description": "Person's surname" },
                    "birth_year": { "type": "integer", "description": "Person's birth year" }
                  },
                  "required": ["name", "surname", "birth_year"],
                  "additionalProperties": false
                }
                """));

    private static ChatTool BuildSubmitAnswerTool() =>
        ChatTool.CreateFunctionTool(
            "submit_answer",
            "Submit the final answer: the suspect found near a nuclear power plant.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "name":         { "type": "string",  "description": "Suspect's first name" },
                    "surname":      { "type": "string",  "description": "Suspect's surname" },
                    "access_level": { "type": "integer", "description": "Their security access level" },
                    "power_plant":  { "type": "string",  "description": "Code of the nearest power plant" }
                  },
                  "required": ["name", "surname", "access_level", "power_plant"],
                  "additionalProperties": false
                }
                """));
}
