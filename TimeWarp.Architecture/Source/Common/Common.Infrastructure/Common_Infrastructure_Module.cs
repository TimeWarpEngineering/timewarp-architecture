namespace TimeWarp.Architecture.Common.Infrastructure;

public class CommonInfrastructureModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.TryAddScoped<ICurrenUserService, CurrentUserService>();
  }
}
