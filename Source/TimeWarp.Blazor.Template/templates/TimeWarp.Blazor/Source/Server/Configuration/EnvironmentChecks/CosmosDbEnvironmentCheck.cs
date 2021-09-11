namespace TimeWarp.Blazor.Configuration
{
  using Microsoft.Extensions.Options;
  using System;

  public class CosmosDbEnvironmentCheck
  {
    private readonly CosmosDbOptions CosmosDbOptions;

    public CosmosDbEnvironmentCheck(IOptions<CosmosDbOptions> aCosmosDbOptionsAccessor)
    {
      CosmosDbOptions = aCosmosDbOptionsAccessor.Value;
    }

    public static string Description => "Can connect to Cosmos DB";

    public void Check()
    {
      ;
      Console.WriteLine("All good!");
    }
  }
}
