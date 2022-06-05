namespace TimeWarp.Architecture.Web.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Configuration;

public class WebInfrastructureModule : IModule
{
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection
      .ConfigureOptions<CosmosDbOptions, CosmosDbOptionsValidator>(aConfiguration);
  }
}
