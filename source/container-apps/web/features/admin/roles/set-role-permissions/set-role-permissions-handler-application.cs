#region Purpose
// Server-side handler for SetRolePermissions: protected-core then replace role grants.
#endregion

#region Design
// Task 182-004: 404 when RoleStore has no role (same as GetRole/Update). Protected-core runs
// BEFORE the store write so Administrator cannot lose RolePermissionSeed.AdminPermissions even
// if the dumb IRolePermissionStore would accept the set. Response re-reads the store so clients
// see the canonical stored list (distinct/order from store implementation).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features;
using static TimeWarp.Architecture.Features.Admin.Roles.SetRolePermissions;

public sealed partial class SetRolePermissions
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRolePermissionStore RolePermissionStore;

    public Handler(IRolePermissionStore rolePermissionStore)
    {
      RolePermissionStore = rolePermissionStore
        ?? throw new ArgumentNullException(nameof(rolePermissionStore));
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      if (!RoleStore.Roles.ContainsKey(command.RoleId))
      {
        return GetRole.Handler.RoleNotFound(command.RoleId);
      }

      IReadOnlyList<string> requested = command.PermissionIds ?? [];
      SharedProblemDetails? protectedCore = AdminLockoutGuards.ProtectedCoreConflict(
        command.RoleId,
        requested);
      if (protectedCore is not null)
      {
        return protectedCore;
      }

      await RolePermissionStore
        .SetPermissionIdsForRoleAsync(command.RoleId, requested, cancellationToken)
        .ConfigureAwait(false);

      IReadOnlyList<string> stored = await RolePermissionStore
        .GetPermissionIdsForRoleAsync(command.RoleId, cancellationToken)
        .ConfigureAwait(false);

      return new Response([.. stored]);
    }
  }
}
