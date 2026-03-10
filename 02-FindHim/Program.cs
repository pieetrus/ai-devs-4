using System.Text.Json;
using _02_FindHim;
using _02_FindHim.Services;
using AiDevs.Shared;

DotEnv.Load();

var aiDevs = new AiDevsClient();
var llm = new LlmClient();
var hub = new HubApiClient();

var suspects = SuspectLoader.Load();
Console.WriteLine($"Loaded {suspects.Count} suspects.");

var plants = await LoadPowerPlantsAsync();
Console.WriteLine($"Loaded {plants.Count} power plants: {string.Join(", ", plants.Select(p => p.CityName))}.");

await new FindHimAgent(aiDevs, llm, hub).RunAsync(suspects, plants);

async Task<List<PowerPlant>> LoadPowerPlantsAsync()
{
    await using var stream = await aiDevs.DownloadDataFileAsync("findhim_locations.json");
    using var doc = await JsonDocument.ParseAsync(stream);
    return doc.RootElement
        .GetProperty("power_plants")
        .EnumerateObject()
        .Select(p =>
        {
            var (lat, lon) = PolishCityCoords.Get(p.Name);
            return new PowerPlant(p.Value.GetProperty("code").GetString()!, p.Name, lat, lon);
        })
        .ToList();
}
