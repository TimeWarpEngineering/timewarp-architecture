#region Purpose
// Server-side handler for SetPrincipalRoles: validates principal exists, last-admin, then write.
#endregion

#region Design
// 404 when IPrincipalStore has no principal — admin UI should only offer known rows, but race
// with concurrent delete/quarantine is still possible. Empty RoleIds is allowed (D10): store
// clears the assignment so effective roles become {Member}. Response echoes the stored list
// (not effective) so clients can confirm the write; ListPrincipals re-reads effective roles.
// Route PrincipalId is PrincipalId typed id generated from {PrincipalId:guid}.
// Last-admin (task 182-004): before write, count principals whose *effective* roles grant
// admin.principals.manage. If exactly one such principal is the target and the proposed stored
// roles would leave them without that permission (after Member-default + bootstrap union),
// return 409 — prevents lockout of principal management. Uses AdminLockoutGuards +
// IEffectiveRolesResolver + IRolePermissionStore; store stays dumb.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Application;

using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Admin.Principals.SetPrincipalRoles;

public sealed partial class SetPrincipalRoles
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IPrincipalRoleStore PrincipalRoleStore;
    private readonly IEffectiveRolesResolver EffectiveRolesResolver;
    private readonly IRolePermissionStore RolePermissionStore;
    private readonly IOptions<BootstrapAdministratorOptions> BootstrapOptions;

    public Handler(
      IPrincipalStore principalStore,
      IPrincipalRoleStore principalRoleStore,
      IEffectiveRolesResolver effectiveRolesResolver,
      IRolePermissionStore rolePermissionStore,
      IOptions<BootstrapAdministratorOptions> bootstrapOptions)
    {
      PrincipalStore = principalStore;
      PrincipalRoleStore = principalRoleStore;
      EffectiveRolesResolver = effectiveRolesResolver;
      RolePermissionStore = rolePermissionStore;
      BootstrapOptions = bootstrapOptions;
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

      SharedProblemDetails? lastAdmin = await CheckLastAdministratorAsync(
          principalId,
          roleIds,
          cancellationToken)
        .ConfigureAwait(false);
      if (lastAdmin is not null)
      {
        return lastAdmin;
      }

      await PrincipalRoleStore
        .SetRoleIdsAsync(principalId, roleIds, cancellationToken)
        .ConfigureAwait(false);

      IReadOnlyList<Guid> stored = await PrincipalRoleStore
        .GetRoleIdsAsync(principalId, cancellationToken)
        .ConfigureAwait(false);

      return new Response([.. stored]);
    }

    /// <summary>
    /// 409 when this write would remove admin.principals.manage from the only principal
    /// who currently has it (via effective roles + role permission expansion).
    /// </summary>
    private async Task<SharedProblemDetails?> CheckLastAdministratorAsync(
      PrincipalId targetPrincipalId,
      IReadOnlyList<Guid> proposedStoredRoleIds,
      CancellationToken cancellationToken)
    {
      IReadOnlyList<Principal> principals = await PrincipalStore
        .ListPrincipalsAsync(cancellationToken)
        .ConfigureAwait(false);

      List<PrincipalId> currentAdmins = [];
      foreach (Principal candidate in principals)
      {
        IReadOnlyList<Guid> effective = await EffectiveRolesResolver
          .GetEffectiveRoleIdsAsync(candidate.Id, cancellationToken)
          .ConfigureAwait(false);

        bool hasManage = await AdminLockoutGuards
          .RolesGrantPermissionAsync(
            effective,
            PermissionIds.AdminPrincipalsManage,
            RolePermissionStore,
            cancellationToken)
          .ConfigureAwait(false);

        if (hasManage)
        {
          currentAdmins.Add(candidate.Id);
        }
      }

      if (currentAdmins.Count != 1 || currentAdmins[0] != targetPrincipalId)
      {
        return null;
      }

      HashSet<PrincipalId> bootstrapIds = AdminLockoutGuards.ParseBootstrapPrincipalIds(
        BootstrapOptions.Value.BootstrapAdministratorPrincipalIds);

      IReadOnlyList<Guid> proposedEffective = AdminLockoutGuards.SimulateEffectiveRoles(
        targetPrincipalId,
        proposedStoredRoleIds,
        bootstrapIds);

      bool stillHasManage = await AdminLockoutGuards
        .RolesGrantPermissionAsync(
          proposedEffective,
          PermissionIds.AdminPrincipalsManage,
          RolePermissionStore,
          cancellationToken)
        .ConfigureAwait(false);

      return stillHasManage ? null : AdminLockoutGuards.LastAdministratorConflict();
    }

    internal static SharedProblemDetails PrincipalNotFound(PrincipalId principalId) => new()
    {
      Title = "Principal not found",
      Status = 404,
      Detail = $"No principal exists with id '{principalId}'."
    };
  }
}
