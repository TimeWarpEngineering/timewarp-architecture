#region Purpose
// Api-server identity store defaults for agent bearer validation: IPrincipalStore + IAgentTokenStore.
#endregion

#region Design
// Narrower than web's InMemoryIdentityStoresModule: api-server does NOT host passkey/agent-key
// ceremonies or challenge stores — only bearer VALIDATION against the same IAgentTokenStore port
// (no parallel opaque-token stack). In-memory singletons match web's single-instance posture;
// a shared distributed store is required before a token minted on web-server can validate here
// (see documentation/developer/how-to-guides/how-to-agent-identity-host-split-web-vs-api.md).
// Ceremony issuance remains web-server-only (104-004 / 104-030 scope).
#endregion

namespace TimeWarp.Architecture.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Identity;
using TimeWarp.Modules;

public class AgentBearerStoresModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddSingleton<IPrincipalStore, InMemoryPrincipalStore>();
    serviceCollection.AddSingleton<IAgentTokenStore, InMemoryAgentTokenStore>();
  }
}
