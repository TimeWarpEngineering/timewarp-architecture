#region Purpose
// Server-side handler for the GetRole by-id query.
#endregion

#region Design
// Missing id returns a 404-shaped SharedProblemDetails through the OneOf failure channel — the
// expected-failure path stays a value, not an exception, so the endpoint maps it to a status
// code and the SPA renders it without try/catch. PermissionIds come from IRolePermissionStore
// (task 182-004 membership editor); empty when the role has no grants.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features;
using static TimeWarp.Architecture.Features.Admin.Roles.GetRole;

public sealed partial class GetRole
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRolePermissionStore RolePermissionStore;

    public Handler(IRolePermissionStore rolePermissionStore)
    {
      RolePermissionStore = rolePermissionStore
        ?? throw new ArgumentNullException(nameof(rolePermissionStore));
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      if (!RoleStore.Roles.TryGetValue(query.RoleId, out (string Name, string Description) role))
      {
        return RoleNotFound(query.RoleId);
      }

      IReadOnlyList<string> permissionIds = await RolePermissionStore
        .GetPermissionIdsForRoleAsync(query.RoleId, cancellationToken)
        .ConfigureAwait(false);

      return new Response(query.RoleId, role.Name, role.Description, [.. permissionIds]);
    }

    internal static SharedProblemDetails RoleNotFound(Guid roleId) => new()
    {
      Title = "Role not found",
      Status = 404,
      Detail = $"No role exists with id '{roleId}'."
    };
  }
}
