namespace TimeWarp.Architecture.Web.Server;

public class CosmosDbModule : IModule
{
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    ConfigureInfrastructure(aServiceCollection);
  }

  private static void ConfigureInfrastructure(IServiceCollection aServiceCollection)
  {
    IHealthChecksBuilder healthChecksBuilder = aServiceCollection.AddHealthChecks();
    healthChecksBuilder.AddDbContextCheck<CosmosDbContext>
    (
      name: nameof(CosmosDbContext),
      HealthStatus.Unhealthy,
      null,
      PerformCosmosHealthCheck()
    );
    ConfigureEnvironmentChecks(aServiceCollection);
    ConfigureCosmosDb(aServiceCollection);
    aServiceCollection.AddHostedService<CosmosDbContextStartupHostedService>();
  }

  private static void ConfigureEnvironmentChecks(IServiceCollection aServiceCollection)
  {
    aServiceCollection.AddSingleton<CosmosDbEnvironmentCheck>();
    aServiceCollection.CheckEnvironment<CosmosDbEnvironmentCheck>
    (
      CosmosDbEnvironmentCheck.Description,
      async (aCosmosDbEnvironmentCheck) => await aCosmosDbEnvironmentCheck.CheckAsync()
    );
  }

  private static void ConfigureCosmosDb(IServiceCollection aServiceCollection)
  {

    using IServiceScope scope = aServiceCollection.BuildServiceProvider().CreateScope();
    {
      CosmosDbOptions cosmosOptions = scope.ServiceProvider.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

      _ = aServiceCollection.AddDbContext<CosmosDbContext>
      (
        aDbContextOptionsBuilder =>
          aDbContextOptionsBuilder
          .UseCosmos
          (
            accountEndpoint: cosmosOptions.Endpoint,
            accountKey: cosmosOptions.AccessKey,
            databaseName: nameof(CosmosDbContext),
            cosmosOptionsAction: CosmosOptionsAction()
          )
      );
    }

    static Action<Microsoft.EntityFrameworkCore.Infrastructure.CosmosDbContextOptionsBuilder> CosmosOptionsAction()
    {
      return _ => new CosmosClientOptions
      {
        HttpClientFactory = () =>
        {
          HttpMessageHandler httpMessageHandler = new HttpClientHandler()
          {
            ServerCertificateCustomValidationCallback =
              HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
          };

          return new HttpClient(httpMessageHandler);
        },
        ConnectionMode = ConnectionMode.Gateway
      };
    }
  }

  private static Func<CosmosDbContext, CancellationToken, Task<bool>> PerformCosmosHealthCheck() =>
   async (aCosmosDbContext, _) =>
   {
     try
     {
       await aCosmosDbContext.Database.GetCosmosClient().ReadAccountAsync().ConfigureAwait(true);
     }
     catch (HttpRequestException)
     {
       return false;
     }

     return true;
   };
}
