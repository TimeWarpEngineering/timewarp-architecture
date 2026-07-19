#region Purpose
// DI registration hook for the Web.Infrastructure assembly, called unconditionally from Web.Server's Program.
#endregion

#region Design
// Postgres wiring lives in Web.Server's PostgresDbModule behind the `postgres` feature flag, so this
// module is the seam for flag-independent infrastructure services only.
// IPrincipalStore and IWebAuthnChallengeStore are registered here as process-lifetime singletons
// backed by the in-memory implementations shipped in timewarp-identity — per 104-003 scope
// boundary (§11), there is no EF-backed store yet (that is a separate, later task) and no
// distributed challenge store; a single web-server instance is the deployment assumption these
// implementations make.
#endregion

namespace TimeWarp.Architecture.Web.Infrastructure;

public class WebInfrastructureModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddSingleton<IPrincipalStore, InMemoryPrincipalStore>();
    serviceCollection.AddSingleton<IWebAuthnChallengeStore, InMemoryWebAuthnChallengeStore>();
  }
}
