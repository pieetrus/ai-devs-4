using System.Text.Json;
using System.Text.Json.Serialization;

namespace _02_FindHim.Services;

internal static class SuspectLoader
{
    public static List<Suspect> Load(string path = "02-FindHim/data/suspects.json")
    {
        var json = File.ReadAllText(path);
        var people = JsonSerializer.Deserialize<List<PersonAnswer>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        return people.Select(p => new Suspect(p.Name, p.Surname, p.Born)).ToList();
    }
}

file record PersonAnswer(
    string Name, string Surname, string Gender,
    int Born, string City,
    [property: JsonPropertyName("tags")] List<string> Tags);
