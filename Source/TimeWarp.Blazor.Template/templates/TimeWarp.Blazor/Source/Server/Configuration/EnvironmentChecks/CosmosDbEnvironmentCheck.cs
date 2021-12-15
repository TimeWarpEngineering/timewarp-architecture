namespace TimeWarp.Blazor.Configuration
{
  using Microsoft.EntityFrameworkCore;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;
  using Oakton;
  using System;
  using System.Net.Http;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Data;

  public class CosmosDbEnvironmentCheck
  {
    private readonly CosmosDbOptions CosmosDbOptions;
    private readonly IServiceProvider ServiceProvider;
    private readonly ILogger Logger;

    public CosmosDbEnvironmentCheck
    (
      IOptions<CosmosDbOptions> aCosmosDbOptionsAccessor,
      IServiceProvider aServiceProvider,
      ILogger<CosmosDbEnvironmentCheck> aLogger
    )
    {
      CosmosDbOptions = aCosmosDbOptionsAccessor.Value;
      ServiceProvider = aServiceProvider;
      Logger = aLogger;
    }

    public static string Description => "Connecting to Cosmos DB";

    public async Task<bool> CheckAsync()
    {
      Logger.LogInformation($"Start {nameof(SampleEnvironmentCheck)} ");
      
      using IServiceScope scope = ServiceProvider.CreateScope();

      CosmosDbContext cosmosDbContext = scope.ServiceProvider.GetRequiredService<CosmosDbContext>();

      try
      {
        await cosmosDbContext.Database.GetCosmosClient().ReadAccountAsync().ConfigureAwait(true);
      }
      catch (HttpRequestException)
      {
        return false;
      }

      Logger.LogInformation($"Completed {nameof(SampleEnvironmentCheck)} ");
      return true;
    }
  }
}
