#region Purpose
// Shared fakes for the permission evaluator runfiles: a held-scopes source and an evaluator factory.
#endregion

namespace TimeWarp.Architecture.Authorization
{

  using Microsoft.Extensions.Options;
  using TimeWarp.Architecture.Features.Admin.Roles.Application;
  using TimeWarp.Architecture.Features.Admin.Roles.Domain;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Identity;

  /// <summary>
  /// Host-free fake for <see cref="IAgentPermissionScopeSource"/> (not IAgentCallerContext —
  /// that type collides under JARIBU_MULTI web+api).
  /// </summary>
  internal sealed class FakeAgentPermissionScopeSource : IAgentPermissionScopeSource
  {
    public static FakeAgentPermissionScopeSource Empty { get; } = new(null, null);

    private readonly PrincipalId? AmbientPrincipalId;
    private readonly IReadOnlyList<string>? Scopes;

    private FakeAgentPermissionScopeSource(PrincipalId? ambientPrincipalId, IReadOnlyList<string>? scopes)
    {
      AmbientPrincipalId = ambientPrincipalId;
      Scopes = scopes;
    }

    public static FakeAgentPermissionScopeSource For(PrincipalId principalId, IReadOnlyList<string> scopes) =>
      new(principalId, scopes);

    public IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId)
    {
      if (AmbientPrincipalId is null || Scopes is null || AmbientPrincipalId != principalId)
      {
        return null;
      }

      return Scopes;
    }
  }

  /// <summary>
  /// Writes role grants through the real Role aggregate (task 473): the grant read ports are
  /// views over InMemoryRoleStore, so a test changes a role the way SetRolePermissions does.
  /// </summary>
  internal static class RoleGrantFixture
  {
    internal static async Task SetPermissionsAsync(this InMemoryRoleStore roles, Guid roleId, IEnumerable<string> permissionIds)
    {
      Role role = await roles.FindAsync(RoleId.From(roleId)) ?? throw new InvalidOperationException($"No role {roleId}.");
      role.SetPermissions(permissionIds);
      await roles.UpdateAsync(role);
    }

    internal static async Task SetAgentScopesAsync(this InMemoryRoleStore roles, Guid roleId, IEnumerable<string> scopes)
    {
      Role role = await roles.FindAsync(RoleId.From(roleId)) ?? throw new InvalidOperationException($"No role {roleId}.");
      role.SetAgentScopes(scopes);
      await roles.UpdateAsync(role);
    }

    /// <summary>The seeded read port (product roles with RolePermissionSeed grants).</summary>
    internal static InMemoryRolePermissionStore SeededPermissions() => new(new InMemoryRoleStore());
  }

  internal static class PermissionEvaluatorFactory
  {
    internal static PermissionEvaluator CreateEvaluator(
      IPrincipalRoleStore principalRoles,
      IRolePermissionStore rolePermissions,
      string[] bootstrap,
      IAgentPermissionScopeSource? agentScopes = null)
    {
      IOptions<BootstrapAdministratorOptions> options = Options.Create(
        new BootstrapAdministratorOptions
        {
          BootstrapAdministratorPrincipalIds = bootstrap
        });
      EffectiveRolesResolver resolver = new(principalRoles, options, new InMemoryPrincipalStore());
      return new PermissionEvaluator(
        resolver,
        rolePermissions,
        agentScopes ?? FakeAgentPermissionScopeSource.Empty);
    }
  }
}
