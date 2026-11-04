#region Purpose
// Server-side handler for SetRolePermissions: protected-core then replace the role's permissions.
#endregion

#region Design
// Task 182-004: 404 when the role does not exist (same as GetRole/Update). Protected core runs
// BEFORE the write so Administrator cannot lose RolePermissionSeed.AdminPermissions; it stays an
// application rule (AdminLockoutGuards, 409), not a domain hook (task 461 resolution). Task 473:
// the write is Role.SetPermissions (full replace, duplicates collapse) saved through IRoleStore;
// the response echoes the role's canonical list after the save.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using static TimeWarp.Architecture.Features.Admin.Roles.SetRolePermissions;

public sealed partial class SetRolePermissions
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRoleStore RoleStore;

    public Handler(IRoleStore roleStore)
    {
      RoleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
    }

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      Role? role = await RoleStore.FindAsync(RoleId.From(command.RoleId), cancellationToken).ConfigureAwait(false);
      if (role is null)
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

      role.SetPermissions(requested);
      await RoleStore.UpdateAsync(role, cancellationToken).ConfigureAwait(false);

      return new Response([.. role.Permissions.Select(permission => permission.PermissionId)]);
    }
  }
}
