namespace TimeWarp.Foundation.Common.Infrastructure;

public class CommonInfrastructureModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.TryAddScoped<ICurrentUserService, CurrentUserService>();
  }
}
