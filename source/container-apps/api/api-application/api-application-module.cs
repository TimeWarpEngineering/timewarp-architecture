#region Purpose
// IModule registration hook for the Api application layer's services.
#endregion

#region Design
// ConfigureServices deliberately throws: the layer has no services to register, and the throw
// makes accidental composition loud instead of silently succeeding. Replace the throw with real
// registrations when the layer gains services.
#endregion

namespace TimeWarp.Architecture.Api.Application;

public class ApiApplicationModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration) =>
    throw new NotImplementedException();
}
