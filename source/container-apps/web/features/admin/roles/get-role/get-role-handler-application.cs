#region Purpose
// Server-side handler for the GetRole by-id query.
#endregion

#region Design
// Missing id returns a 404-shaped SharedProblemDetails through the OneOf failure channel — the
// expected-failure path stays a value, not an exception, so the endpoint maps it to a status
// code and the SPA renders it without try/catch. Task 473: one IRoleStore read loads the Role
// with both child sets. The members come from RoleContractMap.Project, the projection GetRoles
// shares (task 479); the handler wraps it with named arguments.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using static TimeWarp.Architecture.Features.Admin.Roles.GetRole;

public sealed partial class GetRole
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
      Role? role = await RoleStore.FindAsync(RoleId.From(query.RoleId), cancellationToken).ConfigureAwait(false);
      if (role is null)
      {
        return RoleNotFound(query.RoleId);
      }

      RoleContractMap.RoleProjection projection = RoleContractMap.Project(role);
      return new Response(
        roleId: projection.RoleId,
        name: projection.Name,
        description: projection.Description,
        permissions: projection.Permissions,
        agentScopes: projection.AgentScopes);
    }

    internal static SharedProblemDetails RoleNotFound(Guid roleId) => new()
    {
      Title = "Role not found",
      Status = 404,
      Detail = $"No role exists with id '{roleId}'."
    };
  }
}
