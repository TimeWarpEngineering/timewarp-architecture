#region Purpose
// Shared fakes for the permission evaluator runfiles: a held-scopes source and an evaluator factory.
#endregion

namespace TimeWarp.Architecture.Authorization
{

  using Microsoft.Extensions.Options;
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
