#region Purpose
// Web-app port for principal→role assignment (not TimeWarp.Identity Principal roles).
#endregion

#region Design
// Task 147-004 D1: roles live in the web app store, not on the Principal entity / TimeWarp.Identity.
// Empty store for a principal is meaningful (D2): IEffectiveRolesResolver applies the default.
// Member for a human or a missing row, and no role for an agent or a service. This store never
// invents defaults. SetRoleIds replaces the full set
// (D10: empty list allowed). Dual-mode (147-006): InMemoryPrincipalRoleStore singleton default;
// EfPrincipalRoleStore scoped when Postgres connection is present (PostgresDbModule).
// First-administrator claim (product): TryClaimFirstAdministratorAsync is the atomic "empty
// deployment → first human passkey create is Administrator" path. No kill-switch config — an
// empty DB is not an asset to protect; redeploy if a stray first create happened. Bootstrap
// PrincipalIds remain break-glass only. Features substrate namespace (not …Features.Admin.Principals):
// Identity (GetCurrentSession), claims transformation, and Admin.Principals all need the port
// without TWA0009 cross-slice.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>Durable assignment of product role Guids to a principal (web-app concern).</summary>
public interface IPrincipalRoleStore
{
  /// <summary>Stored role ids only — empty when nothing has been assigned yet.</summary>
  Task<IReadOnlyList<Guid>> GetRoleIdsAsync(PrincipalId principalId, CancellationToken cancellationToken = default);

  /// <summary>Replace stored roles. The resolver applies Member for a roleless human and no role for a roleless agent.</summary>
  Task SetRoleIdsAsync(
    PrincipalId principalId,
    IReadOnlyList<Guid> roleIds,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// If no stored principal yet holds <see cref="RoleIds.Administrator"/>, assigns
  /// Administrator + Member to <paramref name="principalId"/> and returns true.
  /// Otherwise leaves the store unchanged and returns false.
  /// Implementations serialize the check+write so concurrent first creates do not all win.
  /// </summary>
  Task<bool> TryClaimFirstAdministratorAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default);
}
