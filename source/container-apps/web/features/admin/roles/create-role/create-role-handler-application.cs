#region Purpose
// Server-side handler for the CreateRole command.
#endregion

#region Design
// Task 473: Role.Create mints the RoleId and checks the details; IRoleStore.AddAsync saves. The
// new role starts with no permissions and no agent scopes. Input validation does NOT belong here —
// FluentValidationBehavior already ran the contract's Validator (shared RoleDetailsValidator +
// AuthApiRequestValidator) before this executes. The response mapping (role id) is hand-written.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public sealed partial class CreateRole
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
      var role = Role.Create(command.Name, command.Description);
      await RoleStore.AddAsync(role, cancellationToken).ConfigureAwait(false);

      return new Response(roleId: role.Id.Value);
    }
  }
}
