#region Purpose
// Default in-process IPermissionEvaluator: principal → effective roles → role permissions.
#endregion

#region Design
// Task 182-001: expands only for identity-session and mock-identity-session
// (AuthenticationSchemeNames). agent-token and unknown/null schemes return empty — agents must
// not inherit human role expansion (182-006 will map agent scopes to permission bundles).
// Uses IEffectiveRolesResolver (Member default + bootstrap union) so first-admin and empty-store
// semantics stay single-source with RequireRole claims transformation until enforcement swaps.
// Output ordered by PermissionIds.All then any unknown grants (stable for session / tests).
// Scoped DI: depends on scoped IEffectiveRolesResolver (and scoped EfRolePermissionStore under
// postgres). No memoization beyond scoped lifetime — rebundle takes effect next request.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>In-process permission expansion via roles + <see cref="IRolePermissionStore"/>.</summary>
public sealed class PermissionEvaluator : IPermissionEvaluator
{
  private readonly IEffectiveRolesResolver EffectiveRolesResolver;
  private readonly IRolePermissionStore RolePermissionStore;

  public PermissionEvaluator(
    IEffectiveRolesResolver effectiveRolesResolver,
    IRolePermissionStore rolePermissionStore)
  {
    EffectiveRolesResolver = effectiveRolesResolver
      ?? throw new ArgumentNullException(nameof(effectiveRolesResolver));
    RolePermissionStore = rolePermissionStore
      ?? throw new ArgumentNullException(nameof(rolePermissionStore));
  }

  public async Task<bool> HasPermissionAsync(
    PrincipalId principalId,
    string? authenticationScheme,
    string permissionId,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

    IReadOnlyList<string> permissions = await GetPermissionsAsync(
        principalId,
        authenticationScheme,
        cancellationToken)
      .ConfigureAwait(false);

    return permissions.Contains(permissionId, StringComparer.Ordinal);
  }

  public async Task<IReadOnlyList<string>> GetPermissionsAsync(
    PrincipalId principalId,
    string? authenticationScheme,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!IsHumanSessionScheme(authenticationScheme))
    {
      return Array.Empty<string>();
    }

    IReadOnlyList<Guid> roleIds = await EffectiveRolesResolver
      .GetEffectiveRoleIdsAsync(principalId, cancellationToken)
      .ConfigureAwait(false);

    HashSet<string> granted = new(StringComparer.Ordinal);
    foreach (Guid roleId in roleIds)
    {
      IReadOnlyList<string> forRole = await RolePermissionStore
        .GetPermissionIdsForRoleAsync(roleId, cancellationToken)
        .ConfigureAwait(false);
      foreach (string permissionId in forRole)
      {
        granted.Add(permissionId);
      }
    }

    if (granted.Count == 0)
    {
      return Array.Empty<string>();
    }

    // Catalog order first, then any non-registry grants (admin UI / future custom ids).
    List<string> ordered = [];
    foreach (string catalogId in PermissionIds.All)
    {
      if (granted.Remove(catalogId))
      {
        ordered.Add(catalogId);
      }
    }

    if (granted.Count > 0)
    {
      ordered.AddRange(granted.OrderBy(static id => id, StringComparer.Ordinal));
    }

    return ordered;
  }

  private static bool IsHumanSessionScheme(string? authenticationScheme) =>
    authenticationScheme is AuthenticationSchemeNames.IdentitySession
      or AuthenticationSchemeNames.MockIdentitySession;
}
