#region Purpose
// Server-side handler for the GetRoles list query.
#endregion

#region Design
// Task 473: one IRoleStore.ListAsync read loads every Role with both child sets (the list is
// small: four product roles plus admin-created ones), so there is no per-row grant query.
// Ordering by name keeps the list stable for UI and tests. Search (task 345) keeps roles whose
// name contains the trimmed term, ignoring case. TotalCount equals the number of matching rows —
// the handler does not implement the contract's OpenData paging parameters. The entity-to-DTO
// mapping is hand-written here on purpose (task 476 evaluates generating it); AgentScopes are
// ordered like AgentScopes.All (task 466).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using static TimeWarp.Architecture.Features.Admin.Roles.GetRoles;

public sealed partial class GetRoles
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRoleStore RoleStore;

    public Handler(IRoleStore roleStore)
    {
      RoleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
    }

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
      IReadOnlyList<Role> roles = await RoleStore.ListAsync(cancellationToken).ConfigureAwait(false);

      List<RoleDto> items =
      [
        .. roles
          .Where(role => search is null || role.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
          .OrderBy(role => role.Name)
          .Select(role => new RoleDto(
            role.Id.Value,
            role.Name,
            role.Description,
            [.. role.Permissions.Select(permission => permission.PermissionId)],
            AgentScopeOrder.OrderKnown([.. role.AgentScopes.Select(scope => scope.Scope)])))
      ];

      return new Response(items.Count, [.. items]);
    }
  }
}
