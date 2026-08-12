#region Purpose
// Server-side handler for SetPrincipalRoles: validates principal exists, replaces stored roles.
#endregion

#region Design
// 404 when IPrincipalStore has no principal — admin UI should only offer known rows, but race
// with concurrent delete/quarantine is still possible. Empty RoleIds is allowed (D10): store
// clears the assignment so effective roles become {Member}. Response echoes the stored list
// (not effective) so clients can confirm the write; ListPrincipals re-reads effective roles.
// Route PrincipalId is PrincipalId typed id generated from {PrincipalId:guid}.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Application;

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Admin.Principals.SetPrincipalRoles;

public sealed partial class SetPrincipalRoles
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IPrincipalRoleStore PrincipalRoleStore;

    public Handler(IPrincipalStore principalStore, IPrincipalRoleStore principalRoleStore)
    {
      PrincipalStore = principalStore;
      PrincipalRoleStore = principalRoleStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      // Route segment is Guid (ApiRoute :guid); convert to typed id like RevokeCredential.
      var principalId = PrincipalId.From(command.PrincipalId);

      Principal? principal = await PrincipalStore
        .GetPrincipalAsync(principalId, cancellationToken)
        .ConfigureAwait(false);

      if (principal is null)
      {
        return PrincipalNotFound(principalId);
      }

      IReadOnlyList<Guid> roleIds = command.RoleIds ?? [];
      await PrincipalRoleStore
        .SetRoleIdsAsync(principalId, roleIds, cancellationToken)
        .ConfigureAwait(false);

      IReadOnlyList<Guid> stored = await PrincipalRoleStore
        .GetRoleIdsAsync(principalId, cancellationToken)
        .ConfigureAwait(false);

      return new Response([.. stored]);
    }

    internal static SharedProblemDetails PrincipalNotFound(PrincipalId principalId) => new()
    {
      Title = "Principal not found",
      Status = 404,
      Detail = $"No principal exists with id '{principalId}'."
    };
  }
}
