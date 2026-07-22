#region Purpose
// Server-side handler for the UpdateRole command.
#endregion

#region Design
// Updates the shared in-memory stub; a missing id reuses GetRole's 404-shaped problem details so
// all roles handlers report absence identically.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using static TimeWarp.Architecture.Features.Admin.Roles.UpdateRole;

public sealed partial class UpdateRole
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    public Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      OneOf<Response, SharedProblemDetails> result = RoleStore.Roles.ContainsKey(command.RoleId)
        ? OnUpdate(command)
        : GetRole.Handler.RoleNotFound(command.RoleId);

      return Task.FromResult(result);
    }

    private static OneOf<Response, SharedProblemDetails> OnUpdate(Command command)
    {
      RoleStore.Roles[command.RoleId] = (command.Name, command.Description);
      return new Response();
    }
  }
}
