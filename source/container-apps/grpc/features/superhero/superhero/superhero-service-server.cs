#region Purpose
// Demo implementation of ISuperheroService that fabricates random superheroes.
#endregion

#region Design
// Exists to exercise the code-first gRPC + gRPC-Web path end to end from the Blazor client; no persistence —
// results are random per call by design, so callers must not expect stable data across requests.
// Template consumers replace this body with a real implementation while keeping the shared
// ISuperheroService contract, which clients bind to directly.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

public class SuperheroService : ISuperheroService
{
  private readonly string[] Powers =
   [
    "Super Maggots",
    "Eat Anything",
    "Super Learning",
    "Explosive Farting",
    "Poison Generation",
    "Flight",
    "Invisibility",
    "Super Strength",
    "Fire Manipulation",
    "Super Speed",
    "Telepathy",
    "Hard Light Constructs",
    "Invulnerability",
    "Telekinesis",
    "Shapeshifting"
  ];

  public static string GenerateName(int length)
  {
    var random = new Random();
    string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
    string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
    string name = "";
    name += consonants[random.Next(consonants.Length)].ToUpper();
    name += vowels[random.Next(vowels.Length)];
    int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
    while (b < length)
    {
      name += consonants[random.Next(consonants.Length)];
      b++;
      name += vowels[random.Next(vowels.Length)];
      b++;
    }

    return name;
  }
  public List<int> SuperheroIds = [];
  public Task<SuperheroResponse> GetSuperheroAsync
  (
    SuperheroRequest superheroRequest,
    CallContext callContext = default
  )
  {
    var heroList = new List<SuperheroDto>();
    var randonm = new Random();
    for (int heroNumber = 1; heroNumber <= superheroRequest.NumberOfHeros; heroNumber++)
    {
      int randomAge = randonm.Next(10, 35);
      heroList.Add(new SuperheroDto()
      {
        Id = heroNumber.ToString(),
        Name = GenerateName(randonm.Next(3, 6)),
        Power = Powers[randonm.Next(0, Powers.Length)],
        Age = randomAge,
        BirthDate = DateTime.Now.AddYears(randomAge * -1)
      }
      );
    }

    var response = new SuperheroResponse()
    {
      Superheros = heroList
    };
    return Task.FromResult(response);
  }
}
