#region Purpose
// One permission id a Role grants: an owned value child of the Role aggregate.
#endregion

#region Design
// Value record (equality by PermissionId) owned by Role and mapped onto the existing
// identity.role_permissions (RoleId, PermissionId) rows; the row key is the owner id plus this
// value, so there is no surrogate id. The constructor checks shape only (non-blank, MaxLength);
// catalog membership is the aggregate's rule (Role.SetPermissions and Role.Invariants), because
// the catalog is a Role concern, not a property of a lone string.
// MaxLength 128 equals the column length the table already has. Named PermissionGrant, not
// RolePermission: CA1711 reserves the Permission suffix for System.Security permission types.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

public sealed record PermissionGrant
{
  public const int MaxLength = 128;

  public PermissionGrant(string permissionId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(permissionId.Length, MaxLength, nameof(permissionId));
    PermissionId = permissionId;
  }

  public string PermissionId { get; }
}
