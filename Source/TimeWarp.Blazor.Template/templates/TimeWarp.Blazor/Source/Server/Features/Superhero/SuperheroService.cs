namespace TimeWarp.Blazor.Features.Superheros
{
  using ProtoBuf.Grpc;
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;

  public class SuperheroService : ISuperheroService
  {
    private readonly string[] Powers = new[]
     {
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
    };

    public static string GenerateName(int len)
    {
      var r = new Random();
      string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
      string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
      string Name = "";
      Name += consonants[r.Next(consonants.Length)].ToUpper();
      Name += vowels[r.Next(vowels.Length)];
      int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
      while (b < len)
      {
        Name += consonants[r.Next(consonants.Length)];
        b++;
        Name += vowels[r.Next(vowels.Length)];
        b++;
      }
      return Name;
    }
    public List<int> SuperheroIds = new List<int>();
    public Task<SuperheroResponse> GetSuperheroAsync
    (
      SuperheroRequest aSuperheroRequest,
      CallContext aCallContext = default
    )
    {
      var heroList = new List<SuperheroDto>();
      var randonm = new Random();
      for (int heroNumber = 1; heroNumber <= aSuperheroRequest.NumberOfHeros; heroNumber++)
      {
        int randomAge = randonm.Next(10, 35);
        heroList.Add(new SuperheroDto() {
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
}
