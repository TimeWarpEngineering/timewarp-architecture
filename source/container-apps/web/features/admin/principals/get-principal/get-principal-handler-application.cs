#region Purpose
// Server-side handler for GetPrincipal: one principal with effective roles and a gated profile.
#endregion

#region Design
// 404 when IPrincipalStore has no principal. The profile is read through IProfileDirectory
// (platform port, TWA0009) only when the caller holds admin.profiles.read, checked through
// IPermissionEvaluator under the identity-session scheme (both endpoint schemes are human
// session schemes that expand roles identically). Without the permission, or without a profile
// row, Profile is null. This handler never creates a profile.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Application;

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Admin.Principals.GetPrincipal;

public sealed partial class GetPrincipal
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IEffectiveRolesResolver EffectiveRolesResolver;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IPermissionEvaluator PermissionEvaluator;
    private readonly IProfileDirectory ProfileDirectory;

    public Handler(
      IPrincipalStore principalStore,
      IEffectiveRolesResolver effectiveRolesResolver,
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IPermissionEvaluator permissionEvaluator,
      IProfileDirectory profileDirectory)
    {
      PrincipalStore = principalStore;
      EffectiveRolesResolver = effectiveRolesResolver;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      PermissionEvaluator = permissionEvaluator;
      ProfileDirectory = profileDirectory;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      var principalId = PrincipalId.From(query.PrincipalId);
      Principal? principal = await PrincipalStore
        .GetPrincipalAsync(principalId, cancellationToken)
        .ConfigureAwait(false);
      if (principal is null)
      {
        return PrincipalProblems.NotFound(principalId);
      }

      IReadOnlyList<Guid> roles = await EffectiveRolesResolver
        .GetEffectiveRoleIdsAsync(principalId, cancellationToken)
        .ConfigureAwait(false);

      PrincipalProfileDto? profile = null;
      if (await CanReadProfilesAsync(cancellationToken).ConfigureAwait(false))
      {
        IReadOnlyDictionary<Guid, ProfileDirectoryEntry> found = await ProfileDirectory
          .FindManyAsync([principalId.Value], cancellationToken)
          .ConfigureAwait(false);
        if (found.TryGetValue(principalId.Value, out ProfileDirectoryEntry? entry))
        {
          profile = new PrincipalProfileDto(
            entry.DisplayName,
            entry.Email,
            entry.Language,
            entry.Region,
            entry.Theme,
            entry.Notifications);
        }
      }

      return new Response(new PrincipalDetailDto(
        principal.Id,
        principal.Kind,
        principal.TrustTier,
        principal.IsActive,
        principal.IsQuarantined,
        [.. roles],
        profile));
    }

    private async Task<bool> CanReadProfilesAsync(CancellationToken cancellationToken)
    {
      PrincipalId? caller = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (caller is null)
      {
        return false;
      }

      return await PermissionEvaluator
        .HasPermissionAsync(
          caller.Value,
          AuthenticationSchemeNames.IdentitySession,
          PermissionIds.AdminProfilesRead,
          cancellationToken)
        .ConfigureAwait(false);
    }
  }
}
