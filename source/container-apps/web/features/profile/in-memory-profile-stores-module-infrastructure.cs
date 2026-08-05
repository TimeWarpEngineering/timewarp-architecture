#region Purpose
// Profile concern's in-memory store default: registers IProfileStore for zero-infra hosts.
#endregion

#region Design
// Task 148 D4: mirrors InMemoryIdentityStoresModule — a concern-local module called from
// Web.Server Program so Program stays free of per-store lines. PostgresDbModule replaces
// IProfileStore with scoped EfProfileStore when a connection string is present; skip-mode
// keeps this singleton. Only the durable Profile port is registered here (avatar is not stored).
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Infrastructure;

using TimeWarp.Architecture.Features.Profiles.Application;

/// <summary>Registers the zero-infra IProfileStore default.</summary>
public sealed class InMemoryProfileStoresModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    _ = configuration;
    serviceCollection.AddSingleton<IProfileStore, InMemoryProfileStore>();
  }
}
