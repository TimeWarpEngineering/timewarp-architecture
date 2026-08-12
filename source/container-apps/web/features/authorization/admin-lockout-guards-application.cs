#region Purpose
// Shared lockout helpers: protected-core on Administrator permissions and last-admin detection.
#endregion

#region Design
// Task 182-004: guards live in handlers (not dumb stores). This type holds pure checks and
// SharedProblemDetails factories so SetRolePermissions and SetPrincipalRoles stay thin and
// host-free tests can exercise the rules without spinning a host.
// Protected-core: system role Administrator must retain RolePermissionSeed.AdminPermissions.
// Last-admin: demoting the sole principal who can grant admin.principals.manage → 409.
// RolesGrantPermissionAsync expands role → permission via IRolePermissionStore only (no scheme
// gate) so admin UI writes evaluate the same membership the evaluator would for human sessions.
// SimulateEffectiveRoles mirrors EffectiveRolesResolver (empty store → Member; bootstrap union)
// so proposed SetPrincipalRoles outcomes match post-write resolution without mutating the store.
// Features substrate — Admin.Roles and Admin.Principals both need it without TWA0009.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>Lockout rules for role-permission and principal-role membership edits.</summary>
public static class AdminLockoutGuards
{
  /// <summary>
  /// When <paramref name="roleId"/> is Administrator, requires every
  /// <see cref="RolePermissionSeed.AdminPermissions"/> id to be present in
  /// <paramref name="requestedPermissionIds"/>. Returns 409 problem when any are missing.
  /// </summary>
  public static SharedProblemDetails? ProtectedCoreConflict(
    Guid roleId,
    IReadOnlyList<string> requestedPermissionIds)
  {
    ArgumentNullException.ThrowIfNull(requestedPermissionIds);

    if (roleId != RoleIds.Administrator)
    {
      return null;
    }

    var requested = new HashSet<string>(requestedPermissionIds, StringComparer.Ordinal);
    var missing = RolePermissionSeed.AdminPermissions
      .Where(permissionId => !requested.Contains(permissionId))
      .ToList();

    if (missing.Count == 0)
    {
      return null;
    }

    return new SharedProblemDetails
    {
      Title = "Protected core permissions",
      Status = 409,
      Detail =
        "The Administrator role must retain all core admin permissions "
        + $"({string.Join(", ", RolePermissionSeed.AdminPermissions)}). "
        + $"Missing: {string.Join(", ", missing)}."
    };
  }

  /// <summary>409 when removing the last principal who can manage principal roles.</summary>
  public static SharedProblemDetails LastAdministratorConflict() => new()
  {
    Title = "Last administrator",
    Status = 409,
    Detail =
      "Cannot remove the last principal who holds a role granting "
      + $"{PermissionIds.AdminPrincipalsManage}. Grant that permission to another principal first."
  };

  /// <summary>
  /// True when any of <paramref name="roleIds"/> grants <paramref name="permissionId"/>
  /// via <paramref name="rolePermissionStore"/>. Empty role list is treated as
  /// <see cref="RoleIds.Member"/> (effective default).
  /// </summary>
  public static async Task<bool> RolesGrantPermissionAsync(
    IReadOnlyList<Guid> roleIds,
    string permissionId,
    IRolePermissionStore rolePermissionStore,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);
    ArgumentNullException.ThrowIfNull(rolePermissionStore);

    IReadOnlyList<Guid> effectiveRoles = roleIds.Count == 0
      ? [RoleIds.Member]
      : roleIds;

    foreach (Guid roleId in effectiveRoles)
    {
      IReadOnlyList<string> granted = await rolePermissionStore
        .GetPermissionIdsForRoleAsync(roleId, cancellationToken)
        .ConfigureAwait(false);

      if (granted.Contains(permissionId, StringComparer.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Mirrors <see cref="EffectiveRolesResolver"/> for a proposed stored-role write without
  /// mutating the store: empty → Member; bootstrap principals also get Administrator+Member.
  /// </summary>
  public static IReadOnlyList<Guid> SimulateEffectiveRoles(
    PrincipalId principalId,
    IReadOnlyList<Guid> storedRoleIds,
    IReadOnlySet<PrincipalId> bootstrapPrincipalIds)
  {
    ArgumentNullException.ThrowIfNull(storedRoleIds);
    ArgumentNullException.ThrowIfNull(bootstrapPrincipalIds);

    HashSet<Guid> effective = storedRoleIds.Count == 0
      ? [RoleIds.Member]
      : [.. storedRoleIds];

    if (bootstrapPrincipalIds.Contains(principalId))
    {
      effective.Add(RoleIds.Administrator);
      effective.Add(RoleIds.Member);
    }

    return RoleIds.All.Where(effective.Contains).ToArray();
  }

  /// <summary>Parse bootstrap principal id strings (invalid Guids ignored — same as resolver).</summary>
  public static HashSet<PrincipalId> ParseBootstrapPrincipalIds(IEnumerable<string>? rawIds)
  {
    var set = new HashSet<PrincipalId>();
    foreach (string raw in rawIds ?? [])
    {
      if (Guid.TryParse(raw, out Guid guid) && guid != Guid.Empty)
      {
        set.Add(PrincipalId.From(guid));
      }
    }

    return set;
  }
}
