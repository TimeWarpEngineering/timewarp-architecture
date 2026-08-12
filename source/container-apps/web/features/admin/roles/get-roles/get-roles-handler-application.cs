#region Purpose
// Server-side handler for the GetRoles list query.
#endregion

#region Design
// Reads the shared in-memory stub (role-store.cs); ordering by name keeps the list stable for
// UI and tests. PermissionIds per row come from IRolePermissionStore (task 182-004 membership
// matrix on RolesListPage). TotalCount equals the full store size — the demo does not implement
// the contract's OpenData paging parameters; a real repository-backed handler would.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features;
using static TimeWarp.Architecture.Features.Admin.Roles.GetRoles;

public sealed partial class GetRoles
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
      List<RoleDto> items = [];
      foreach ((Guid roleId, (string Name, string Description) role) in RoleStore.Roles.OrderBy(pair => pair.Value.Name))
      {
        IReadOnlyList<string> permissionIds = await RolePermissionStore
          .GetPermissionIdsForRoleAsync(roleId, cancellationToken)
          .ConfigureAwait(false);

        items.Add(new RoleDto(roleId, role.Name, role.Description, [.. permissionIds]));
      }

      return new Response(items.Count, [.. items]);
    }
  }
}
