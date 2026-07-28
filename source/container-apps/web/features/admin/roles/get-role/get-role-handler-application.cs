#region Purpose
// Server-side handler for the GetRole by-id query.
#endregion

#region Design
// Missing id returns a 404-shaped SharedProblemDetails through the OneOf failure channel — the
// expected-failure path stays a value, not an exception, so the endpoint maps it to a status
// code and the SPA renders it without try/catch.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using static TimeWarp.Architecture.Features.Admin.Roles.GetRole;

public sealed partial class GetRole
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    public Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      OneOf<Response, SharedProblemDetails> result = RoleStore.Roles.TryGetValue(query.RoleId, out (string Name, string Description) role)
        ? new Response(query.RoleId, role.Name, role.Description)
        : RoleNotFound(query.RoleId);

      return Task.FromResult(result);
    }

    internal static SharedProblemDetails RoleNotFound(Guid roleId) => new()
    {
      Title = "Role not found",
      Status = 404,
      Detail = $"No role exists with id '{roleId}'."
    };
  }
}
