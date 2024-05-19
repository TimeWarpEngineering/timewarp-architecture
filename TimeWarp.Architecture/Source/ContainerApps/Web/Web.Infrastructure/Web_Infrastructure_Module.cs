namespace TimeWarp.Architecture.Web.Infrastructure;

public class WebInfrastructureModule : IModule
{
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection
      .ConfigureOptions<CosmosDbOptions, CosmosDbOptionsValidator>(aConfiguration);
  }
}
