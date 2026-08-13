#region Purpose
// Default in-process IPermissionEvaluator: human roles or agent scopes → permissions.
#endregion

#region Design
// Task 182-001/182-006: scheme-aware expansion.
// Human schemes (identity-session, mock-identity-session): principal → effective roles →
// IRolePermissionStore (IEffectiveRolesResolver shared with PrincipalRoleClaimsTransformation).
// Agent-token: scopes only via IAgentPermissionScopeSource + AgentScopePermissionSeed — NEVER
// EffectiveRolesResolver or RolePermissionStore (no human role inheritance for agents).
// Fail-closed: missing ambient scopes (null) → empty. Agent path is pure in-memory (no
// single-flight cache); human path keeps task-183 in-flight single-flight so concurrent
// Blazor SSR policy checks do not race the scoped DbContext. Completed expansions are
// evicted: the evaluator is scoped to a Blazor Server circuit, and a sticky cache would
// keep pre-Save grants (Developer nav stays hidden until refresh).
// IAgentPermissionScopeSource (not IAgentCallerContext) so Features stays free of dual-host
// Abstractions types that collide under JARIBU_MULTI (web + api both define IAgentCallerContext).
// Output ordered by PermissionIds.All then any unknown grants (stable for session / tests).
// Scoped DI: IEffectiveRolesResolver, IRolePermissionStore, IAgentPermissionScopeSource.
#endregion

namespace TimeWarp.Architecture.Features;

using System.Collections.Concurrent;
using TimeWarp.Identity;

/// <summary>
/// In-process permission expansion via roles (humans) or agent scopes (agent-token).
/// </summary>
public sealed class PermissionEvaluator : IPermissionEvaluator
{
  private readonly IEffectiveRolesResolver EffectiveRolesResolver;
  private readonly IRolePermissionStore RolePermissionStore;
  private readonly IAgentPermissionScopeSource AgentPermissionScopeSource;
  private readonly ConcurrentDictionary<(Guid PrincipalId, string Scheme), Lazy<Task<IReadOnlyList<string>>>> ExpansionCache = new();

  public PermissionEvaluator(
    IEffectiveRolesResolver effectiveRolesResolver,
    IRolePermissionStore rolePermissionStore,
    IAgentPermissionScopeSource agentPermissionScopeSource)
  {
    EffectiveRolesResolver = effectiveRolesResolver
      ?? throw new ArgumentNullException(nameof(effectiveRolesResolver));
    RolePermissionStore = rolePermissionStore
      ?? throw new ArgumentNullException(nameof(rolePermissionStore));
    AgentPermissionScopeSource = agentPermissionScopeSource
      ?? throw new ArgumentNullException(nameof(agentPermissionScopeSource));
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

    if (IsAgentTokenScheme(authenticationScheme))
    {
      return ExpandAgentPermissions(principalId);
    }

    if (!IsHumanSessionScheme(authenticationScheme))
    {
      return Array.Empty<string>();
    }

    (Guid PrincipalId, string Scheme) key = (principalId.Value, authenticationScheme!);
    Lazy<Task<IReadOnlyList<string>>> expansion = ExpansionCache.GetOrAdd(
      key,
      _ => new Lazy<Task<IReadOnlyList<string>>>(
        () => ExpandHumanPermissionsAsync(principalId),
        LazyThreadSafetyMode.ExecutionAndPublication));

    try
    {
      return await expansion.Value.ConfigureAwait(false);
    }
    finally
    {
      // Evict only this flight so a later GetOrAdd after a grant change re-expands.
      ExpansionCache.TryRemove(
        new KeyValuePair<(Guid PrincipalId, string Scheme), Lazy<Task<IReadOnlyList<string>>>>(key, expansion));
    }
  }

  private IReadOnlyList<string> ExpandAgentPermissions(PrincipalId principalId)
  {
    IReadOnlyList<string>? scopes = AgentPermissionScopeSource.GetHeldScopesFor(principalId);
    if (scopes is null)
    {
      return Array.Empty<string>();
    }

    return AgentScopePermissionSeed.Expand(scopes);
  }

  private async Task<IReadOnlyList<string>> ExpandHumanPermissionsAsync(PrincipalId principalId)
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

  private static bool IsAgentTokenScheme(string? authenticationScheme) =>
    authenticationScheme is AuthenticationSchemeNames.AgentToken;
}
