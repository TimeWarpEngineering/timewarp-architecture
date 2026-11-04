#region Purpose
// Server-side handler for the UpdateRole command.
#endregion

#region Design
// Task 473: load the Role, UpdateDetails (name and description as a pair), save through
// IRoleStore. A missing id reuses GetRole's 404-shaped problem details so all roles handlers
// report absence identically. Product roles may be renamed; only deletion is refused.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using static TimeWarp.Architecture.Features.Admin.Roles.UpdateRole;

public sealed partial class UpdateRole
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRoleStore RoleStore;

    public Handler(IRoleStore roleStore)
    {
      RoleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
    }

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      Role? role = await RoleStore.FindAsync(RoleId.From(command.RoleId), cancellationToken).ConfigureAwait(false);
      if (role is null)
      {
        return GetRole.Handler.RoleNotFound(command.RoleId);
      }

      role.UpdateDetails(command.Name, command.Description);
      await RoleStore.UpdateAsync(role, cancellationToken).ConfigureAwait(false);
      return new Response();
    }
  }
}
