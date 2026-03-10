namespace _02_FindHim;

internal record Suspect(string Name, string Surname, int BirthYear);

internal record PowerPlant(string Code, string CityName, double Lat, double Lon);

internal record Coordinate(
    [property: System.Text.Json.Serialization.JsonPropertyName("latitude")]  double Lat,
    [property: System.Text.Json.Serialization.JsonPropertyName("longitude")] double Lon);

// Known coordinates for Polish cities hosting power plants
internal static class PolishCityCoords
{
    private static readonly Dictionary<string, (double Lat, double Lon)> Coords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Zabrze"]                  = (50.3249, 18.7857),
        ["Piotrków Trybunalski"]    = (51.4047, 19.6984),
        ["Grudziądz"]               = (53.4838, 18.7633),
        ["Tczew"]                   = (53.7778, 18.7792),
        ["Radom"]                   = (51.4027, 21.1471),
        ["Chelmno"]                 = (53.3500, 18.4333),
        ["Żarnowiec"]               = (54.8500, 18.1167),
    };

    public static (double Lat, double Lon) Get(string city) =>
        Coords.TryGetValue(city, out var c) ? c : throw new KeyNotFoundException($"No coords for city: {city}");
}
