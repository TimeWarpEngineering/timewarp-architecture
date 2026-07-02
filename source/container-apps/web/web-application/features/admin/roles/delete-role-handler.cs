#region Purpose
// Server-side handler for the DeleteRole command.
#endregion

#region Design
// Removes from the shared in-memory stub; a missing id reuses GetRole's 404-shaped problem
// details so all roles handlers report absence identically.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using static TimeWarp.Architecture.Features.Admin.Roles.DeleteRole;

public sealed partial class DeleteRole
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    public Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      OneOf<Response, SharedProblemDetails> result = RoleStore.Roles.TryRemove(command.RoleId, out _)
        ? new Response()
        : GetRole.Handler.RoleNotFound(command.RoleId);

      return Task.FromResult(result);
    }
  }
}
