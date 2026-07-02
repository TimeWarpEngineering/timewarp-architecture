#region Purpose
// DI registration hook for the Web.Infrastructure assembly, called unconditionally from Web.Server's Program.
#endregion

#region Design
// Empty by design: Postgres wiring lives in Web.Server's PostgresDbModule behind the
// `postgres` feature flag, so this module is the seam for flag-independent infrastructure
// services only.
#endregion

namespace TimeWarp.Architecture.Web.Infrastructure;

public class WebInfrastructureModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {

  }
}
