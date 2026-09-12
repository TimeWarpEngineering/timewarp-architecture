#region Purpose
// Decision seam for "does this principal hold permission P?" (roles or agent scopes expand).
#endregion

#region Design
// IPermissionEvaluator is the only authorization decision port. Surfaces enforce permissions
// (PermissionIds strings as policy names) — never product role Guids. Roles remain editable
// bundles in IRolePermissionStore. PermissionRequirementHandler and GetCurrentSession must
// route through this interface so an external PDP (OpenFGA/Cedar/SpiceDB) can replace the
// default in-process expansion without rewriting contracts, pages, or SPA AuthorizeView names.
// Scheme-aware: human session schemes expand principal → effective roles → role permissions;
// agent-token expands ambient scopes via IAgentCallerContext + AgentScopePermissionSeed —
// never human role membership. Cookie stays PrincipalId-only — expansion is per-request.
// Swap: register a different IPermissionEvaluator in DI (Replace if the default already ran);
// leave PermissionIds, PermissionRequirementHandler, AddPermissionPolicies, and SPA claim
// projection. Adapter must fail closed on PDP errors, stay scoped, and honor the scheme split.
// Do not add an external PDP to AppHost as a required template dependency. Entra SPA claims
// must use this same evaluator-backed source when that branch is touched — no second map.
// Platform cluster (web/platform/authorization) — product slices consume it freely (TWA0009).
// PermissionIds stay Features substrate.
#endregion

namespace TimeWarp.Architecture.Authorization;

using TimeWarp.Identity;

/// <summary>Evaluates permission grants for a principal under an authentication scheme.</summary>
public interface IPermissionEvaluator
{
  /// <summary>
  /// True when the principal holds <paramref name="permissionId"/> under
  /// <paramref name="authenticationScheme"/> (role expansion for human sessions; scope
  /// expansion for agent-token).
  /// </summary>
  Task<bool> HasPermissionAsync(
    PrincipalId principalId,
    string? authenticationScheme,
    string permissionId,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Expanded permission ids for the principal under the scheme (stable catalog order where known).
  /// Agent-token expands scopes only; unrecognized schemes return empty.
  /// </summary>
  Task<IReadOnlyList<string>> GetPermissionsAsync(
    PrincipalId principalId,
    string? authenticationScheme,
    CancellationToken cancellationToken = default);
}
