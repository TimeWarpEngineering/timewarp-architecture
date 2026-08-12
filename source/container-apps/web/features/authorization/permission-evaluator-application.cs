#region Purpose
// Default in-process IPermissionEvaluator: principal → effective roles → role permissions.
#endregion

#region Design
// Task 182-001: expands only for identity-session and mock-identity-session
// (AuthenticationSchemeNames). agent-token and unknown/null schemes return empty — agents must
// not inherit human role expansion (182-006 will map agent scopes to permission bundles).
// Uses IEffectiveRolesResolver (Member default + bootstrap union) so first-admin and empty-store
// semantics stay single-source with PrincipalRoleClaimsTransformation and
// PermissionRequirementHandler / GetCurrentSession (182-003).
// Output ordered by PermissionIds.All then any unknown grants (stable for session / tests).
// Scoped DI: depends on scoped IEffectiveRolesResolver (and scoped EfRolePermissionStore under
// postgres). No memoization beyond scoped lifetime — rebundle takes effect next request.
// Task 183: WITHIN the scope, expansion is single-flighted per (principal, scheme). Blazor SSR
// of an authorized page evaluates several policies concurrently (AuthorizeRouteView + nav
// AuthorizeViews) in ONE request scope; without single-flight those raced concurrent queries on
// the same scoped PostgresDbContext ("A second operation was started on this context") and every
// authenticated page 500'd under postgres. Concurrent callers now await one sequential DB chain.
// The shared expansion runs with CancellationToken.None so one caller's cancellation cannot
// poison the cached task for the others; callers still observe their own token at entry.
#endregion

namespace TimeWarp.Architecture.Features;

using System.Collections.Concurrent;
using TimeWarp.Identity;

/// <summary>In-process permission expansion via roles + <see cref="IRolePermissionStore"/>.</summary>
public sealed class PermissionEvaluator : IPermissionEvaluator
{
  private readonly IEffectiveRolesResolver EffectiveRolesResolver;
  private readonly IRolePermissionStore RolePermissionStore;
  private readonly ConcurrentDictionary<(Guid PrincipalId, string Scheme), Lazy<Task<IReadOnlyList<string>>>> ExpansionCache = new();

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

    Lazy<Task<IReadOnlyList<string>>> expansion = ExpansionCache.GetOrAdd(
      (principalId.Value, authenticationScheme!),
      _ => new Lazy<Task<IReadOnlyList<string>>>(
        () => ExpandPermissionsAsync(principalId),
        LazyThreadSafetyMode.ExecutionAndPublication));

    return await expansion.Value.ConfigureAwait(false);
  }

  private async Task<IReadOnlyList<string>> ExpandPermissionsAsync(PrincipalId principalId)
  {
    IReadOnlyList<Guid> roleIds = await EffectiveRolesResolver
      .GetEffectiveRoleIdsAsync(principalId, CancellationToken.None)
      .ConfigureAwait(false);

    HashSet<string> granted = new(StringComparer.Ordinal);
    foreach (Guid roleId in roleIds)
    {
      IReadOnlyList<string> forRole = await RolePermissionStore
        .GetPermissionIdsForRoleAsync(roleId, CancellationToken.None)
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
