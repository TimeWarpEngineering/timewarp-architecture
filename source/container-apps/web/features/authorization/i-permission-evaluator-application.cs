#region Purpose
// Decision seam for "does this principal hold permission P?" (roles expand to permissions).
#endregion

#region Design
// Task 182-001 / disposition: IPermissionEvaluator is the only authorization decision port.
// Handlers (182-002 PermissionRequirement) and GetCurrentSession (182-003) must route through
// this interface so an external PDP (OpenFGA/Cedar) can replace the default in-process
// expansion without rewriting enforcement. Scheme-aware from day one: human session schemes
// expand principal → effective roles → role permissions; agent-token returns empty until
// 182-006 maps scopes to permission bundles. Cookie stays PrincipalId-only (147-004 D8) —
// expansion is per-request, never baked into the identity-session cookie.
// Features substrate so Identity + Admin + server handlers share one port without TWA0009.
// Docs: ADR-0010 (accepted) + how-to-swap-permission-evaluator-for-external-pdp.md
// (consumer PDP swap; no AppHost OpenFGA by default).
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>Evaluates permission grants for a principal under an authentication scheme.</summary>
public interface IPermissionEvaluator
{
  /// <summary>
  /// True when the principal holds <paramref name="permissionId"/> under
  /// <paramref name="authenticationScheme"/> (role expansion for human sessions).
  /// </summary>
  Task<bool> HasPermissionAsync(
    PrincipalId principalId,
    string? authenticationScheme,
    string permissionId,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Expanded permission ids for the principal under the scheme (stable catalog order where known).
  /// Empty for agent-token and unrecognized schemes until scope mapping (182-006).
  /// </summary>
  Task<IReadOnlyList<string>> GetPermissionsAsync(
    PrincipalId principalId,
    string? authenticationScheme,
    CancellationToken cancellationToken = default);
}
