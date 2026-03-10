using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace _01_PeopleTask.Services;

internal static class PeopleFilter
{
    public static List<PersonRow> LoadAndFilter(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = a => a.Header.ToLowerInvariant()
        });

        return csv.GetRecords<PersonRow>()
            .Where(MatchesCriteria)
            .ToList();
    }

    private static bool MatchesCriteria(PersonRow p) =>
        p is { Gender: "M", BirthPlace: "Grudziądz" }
        && BirthYear(p) is var year && 2026 - year is >= 20 and <= 40;

    private static int BirthYear(PersonRow p) => int.Parse(p.BirthDate[..4]);
}