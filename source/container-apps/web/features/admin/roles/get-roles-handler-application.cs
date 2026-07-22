#region Purpose
// Server-side handler for the GetRoles list query.
#endregion

#region Design
// Reads the shared in-memory stub (role-store.cs); ordering by name keeps the list stable for
// UI and tests. TotalCount equals the full store size — the demo does not implement the
// contract's OpenData paging parameters; a real repository-backed handler would.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using static TimeWarp.Architecture.Features.Admin.Roles.GetRoles;

public sealed partial class GetRoles
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    public Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      RoleDto[] items = RoleStore.Roles
        .Select(pair => new RoleDto(pair.Key, pair.Value.Name, pair.Value.Description))
        .OrderBy(dto => dto.Name)
        .ToArray();

      return Task.FromResult((OneOf<Response, SharedProblemDetails>)new Response(items.Length, items));
    }
  }
}
