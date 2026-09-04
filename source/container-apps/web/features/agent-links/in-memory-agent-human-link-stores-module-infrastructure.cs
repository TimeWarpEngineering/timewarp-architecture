#region Purpose
// Agent-links concern's in-memory store default: registers IAgentHumanLinkStore for zero-infra hosts.
#endregion

#region Design
// Concern-local module called from Web.Server Program so Program stays free of per-store lines.
// PostgresDbModule replaces IAgentHumanLinkStore with scoped EfAgentHumanLinkStore when a
// connection string is present; skip-mode keeps this singleton.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Infrastructure;

using TimeWarp.Architecture.Features.AgentLinks.Application;

/// <summary>Registers the zero-infra IAgentHumanLinkStore default.</summary>
public sealed class InMemoryAgentHumanLinkStoresModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    _ = configuration;
    serviceCollection.AddSingleton<IAgentHumanLinkStore, InMemoryAgentHumanLinkStore>();
  }
}
