#region Purpose
// Server-side handler for the CreateRole command.
#endregion

#region Design
// Storage is the shared in-memory stub (see role-store.cs) so the roles CRUD handlers compose.
// Input validation does NOT belong here — FluentValidationBehavior already ran the contract's
// Validator (shared RoleDetailsValidator + AuthApiRequestValidator) before this executes.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public sealed partial class CreateRole
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    public Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      var roleId = Guid.NewGuid();
      RoleStore.Roles.TryAdd(roleId, (command.Name, command.Description));

      return Task.FromResult((OneOf<Response, SharedProblemDetails>)new Response(roleId));
    }
  }
}
