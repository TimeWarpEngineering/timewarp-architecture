namespace TimeWarp.Architecture.Web.Server;

public class CosmosDbModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    ConfigureInfrastructure(serviceCollection);
  }

  private static void ConfigureInfrastructure(IServiceCollection serviceCollection)
  {
    // serviceCollection.AddHostedService<CosmosDbContextStartupHostedService>();
  }

  public static void ConfigureHostApplicationBuilder(IHostApplicationBuilder builder)
  {
    builder.AddAzureCosmosClient(CosmosDbConnectionStringName);
    builder.AddCosmosDbContext<CosmosDbContext>(CosmosDbConnectionStringName, CosmosDbDatabaseName);
  }
}
