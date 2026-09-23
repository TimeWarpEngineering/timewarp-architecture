#region Purpose
// Api-server identity store defaults for agent bearer validation: IPrincipalStore + IAgentTokenStore.
#endregion

#region Design
// Agent identity is split by capability, not duplicated as two full identity systems.
// Ceremonies that mint principals, credentials, and tokens stay on web-server (passkey,
// agent-key register, token issuance, GET api/identity/agent/me). api-server hosts the same
// VALIDATION stack (scheme agent-token, claims timewarp:scope / timewarp:principal_id,
// IAgentTokenStore + IPrincipalStore) so agents can call protected product routes here.
// In-memory singletons are per-process: a token minted on web-server is not visible here.
// Dual-host / multi-instance production needs a shared store behind the same ports — the
// authentication handler shape does not change. Integration tests seed principal + Issue
// on this host. String enums go through ContractSerializationDefaults on both hosts.
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
    // Last-used stamps for bearer validation (task 248-002) — same singleton shape as web.
    serviceCollection.AddSingleton<CredentialUsageRecorder>();
  }
}
