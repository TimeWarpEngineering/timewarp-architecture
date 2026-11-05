#region Purpose
// Projects a Role onto the members its read contracts share, once, for GetRole and GetRoles.
#endregion

#region Design
// GetRole.Response and GetRoles.RoleDto stay separate wire types (Response implements
// IRoleDetails with settable details for the edit form; RoleDto is an immutable list row), but
// both carry the same five members. Project reads the Role once into RoleProjection, and each
// handler wraps it with named arguments, so the domain-to-member list lives here only
// (tw-aggregate-pattern, "Projection and mutation calls stay hand-written"; task 479).
// Permissions keep the stored order; Role.AgentScopes already reads back in catalog order
// (task 478), so the projection takes it as is and does not reorder.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;

internal static class RoleContractMap
{
  /// <summary>The Role members GetRole.Response and GetRoles.RoleDto share.</summary>
  public readonly record struct RoleProjection
  (
    Guid RoleId,
    string Name,
    string Description,
    List<string> Permissions,
    List<string> AgentScopes
  );

  /// <summary>Reads the shared read-contract members from a Role, with fresh lists on each call.</summary>
  public static RoleProjection Project(Role role) =>
    new
    (
      RoleId: role.Id.Value,
      Name: role.Name,
      Description: role.Description,
      Permissions: [.. role.Permissions.Select(permission => permission.PermissionId)],
      AgentScopes: [.. role.AgentScopes.Select(scope => scope.Scope)]
    );
}
