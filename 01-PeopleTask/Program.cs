using System.Text.Json;
using _01_PeopleTask;
using _01_PeopleTask.Services;
using AiDevs.Shared;

DotEnv.Load();

var client = new AiDevsClient();
await using var csvStream = await client.DownloadDataFileAsync("people.csv");

var people = PeopleFilter.LoadAndFilter(csvStream);
Console.WriteLine($"Filtered {people.Count} people from CSV.");

var tagging = await new JobTaggerAgent().TagJobsAsync(people);

var answer = tagging.Results
    .Where(r => r.Tags.Contains("transport"))
    .Select(r => people[r.Id - 1] is var p
        ? new PersonAnswer(p.Name, p.Surname, p.Gender, int.Parse(p.BirthDate[..4]), p.BirthPlace, r.Tags)
        : null!)
    .ToList();

Console.WriteLine($"People with 'transport' tag: {answer.Count}");

const string suspectsFile = "02-FindHim/data/suspects.json";
Directory.CreateDirectory(Path.GetDirectoryName(suspectsFile)!);
await File.WriteAllTextAsync(suspectsFile, JsonSerializer.Serialize(answer, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Suspects saved to {suspectsFile}");

var result = await new AiDevsClient().VerifyAsync("people", answer);

Console.WriteLine($"Verify response: {result}");
