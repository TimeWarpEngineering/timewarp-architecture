#region Purpose
// The identity concern's in-memory store defaults: a module registering zero-infra implementations
// of the identity persistence ports, called unconditionally from Web.Server's Program.
#endregion

#region Design
// A module is a concern's registration manifest and lives in the concern's folder; this one is the
// identity slice's flag-independent persistence defaults. Postgres wiring lives in the platform
// postgres cluster's PostgresDbModule behind the `postgres` feature flag.
// Durability (task 104-032):
//   - IPrincipalStore defaults here to singleton InMemoryPrincipalStore (zero-infra / skip-mode).
//     When PostgresDbModule sees a connection string it replaces this registration with scoped
//     EfPrincipalStore so principals/credentials survive restarts.
//   - IWebAuthnChallengeStore / IAgentKeyChallengeStore / IAgentTokenStore stay process-lifetime
//     in-memory singletons deliberately — ceremony nonces and short-lived bearer grants are
//     ephemeral (Redis later if multi-replica requires shared token state). No distributed store
//     yet; a single web-server instance is the deployment assumption for those three.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Infrastructure;

public class InMemoryIdentityStoresModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    // Default durable-port backend; PostgresDbModule swaps to EfPrincipalStore when connected.
    serviceCollection.AddSingleton<IPrincipalStore, InMemoryPrincipalStore>();
    serviceCollection.AddSingleton<IWebAuthnChallengeStore, InMemoryWebAuthnChallengeStore>();
    serviceCollection.AddSingleton<IAgentKeyChallengeStore, InMemoryAgentKeyChallengeStore>();
    serviceCollection.AddSingleton<IAgentTokenStore, InMemoryAgentTokenStore>();
  }
}
