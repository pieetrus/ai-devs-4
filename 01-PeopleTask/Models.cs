namespace _01_PeopleTask;

internal record PersonRow(string Name, string Surname, string Gender,
    string BirthDate, string BirthPlace, string BirthCountry, string Job);

internal record TaggingResult(List<TaggedItem> Results);

internal record TaggedItem(int Id, List<string> Tags);

internal record PersonAnswer(string Name, string Surname, string Gender,
    int Born, string City, List<string> Tags);