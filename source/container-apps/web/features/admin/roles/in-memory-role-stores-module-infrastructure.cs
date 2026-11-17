#region Purpose
// Roles concern's in-memory store defaults: IRoleStore and the two authorization read ports over it.
#endregion

#region Design
// Task 473: mirrors InMemoryProfileStoresModule, called from Web.Server Program. One
// InMemoryRoleStore singleton backs IRoleStore, IRolePermissionStore, and IRoleAgentScopeStore,
// so a write through the aggregate is what the evaluator and agent-scope approval read next.
// PostgresDbModule replaces all three ports with scoped EF implementations when a connection
// string is present (the concrete singleton stays registered and unused). These registrations
// moved here from InMemoryIdentityStoresModule because their implementations live in this slice.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features.Admin.Roles.Application;

/// <summary>Registers the zero-infra Role store and the role grant read ports over it.</summary>
public sealed class InMemoryRoleStoresModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    _ = configuration;
    serviceCollection.AddSingleton<InMemoryRoleStore>();
    serviceCollection.AddSingleton<IRoleStore>(provider => provider.GetRequiredService<InMemoryRoleStore>());
    serviceCollection.AddSingleton<IRolePermissionStore, InMemoryRolePermissionStore>();
    serviceCollection.AddSingleton<IRoleAgentScopeStore, InMemoryRoleAgentScopeStore>();
  }
}
