using _01_PeopleTask;
using _01_PeopleTask.Services;
using AiDevs.Shared;

var csvPath = args.Length > 0 ? args[0] : "data/people.csv";

var people = PeopleFilter.LoadAndFilter(csvPath);
Console.WriteLine($"Filtered {people.Count} people from CSV.");

var tagging = await new JobTaggerAgent().TagJobsAsync(people);

var answer = tagging.Results
    .Where(r => r.Tags.Contains("transport"))
    .Select(r => people[r.Id - 1] is var p
        ? new PersonAnswer(p.Name, p.Surname, p.Gender, int.Parse(p.BirthDate[..4]), p.BirthPlace, r.Tags)
        : null!)
    .ToList();

Console.WriteLine($"People with 'transport' tag: {answer.Count}");

var result = await new AiDevsClient().VerifyAsync("people", answer);

Console.WriteLine($"Verify response: {result}");
