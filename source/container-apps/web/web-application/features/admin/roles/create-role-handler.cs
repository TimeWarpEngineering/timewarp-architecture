#region Purpose
// Server-side handler for the CreateRole command.
#endregion

#region Design
// Storage is a deliberate in-memory stub: roles are a template demonstration feature with no
// domain persistence yet, and the handler's job here is to complete the contract round-trip
// (validated Command in, Response with the new id out). Replace the store with a repository
// when roles become a real feature; the contract and endpoint do not change.
// Input validation does NOT belong here — FluentValidationBehavior already ran the contract's
// Validator (shared RoleDetailsValidator + AuthApiRequestValidator) before this executes.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using System.Collections.Concurrent;
using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public sealed partial class CreateRole
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private static readonly ConcurrentDictionary<Guid, (string Name, string Description)> Store = new();

    public Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      var roleId = Guid.NewGuid();
      Store.TryAdd(roleId, (command.Name, command.Description));

      return Task.FromResult((OneOf<Response, SharedProblemDetails>)new Response(roleId));
    }
  }
}
