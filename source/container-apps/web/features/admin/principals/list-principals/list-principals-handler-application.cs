#region Purpose
// Server-side handler for ListPrincipals: principal store + effective roles for admin UI.
#endregion

#region Design
// Lists all principals from IPrincipalStore (CreatedAt order from the port) and attaches
// effective RoleIds via IEffectiveRolesResolver so the SPA multi-select matches RequireRole.
// No paging yet — template admin surface is small; OpenData can land later if needed.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Application;

using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Admin.Principals.ListPrincipals;

public sealed partial class ListPrincipals
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IEffectiveRolesResolver EffectiveRolesResolver;

    public Handler(IPrincipalStore principalStore, IEffectiveRolesResolver effectiveRolesResolver)
    {
      PrincipalStore = principalStore;
      EffectiveRolesResolver = effectiveRolesResolver;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      IReadOnlyList<Principal> principals = await PrincipalStore
        .ListPrincipalsAsync(cancellationToken)
        .ConfigureAwait(false);

      var items = new PrincipalSummaryDto[principals.Count];
      for (int i = 0; i < principals.Count; i++)
      {
        Principal principal = principals[i];
        IReadOnlyList<Guid> roles = await EffectiveRolesResolver
          .GetEffectiveRoleIdsAsync(principal.Id, cancellationToken)
          .ConfigureAwait(false);

        items[i] = new PrincipalSummaryDto(
          principal.Id,
          principal.Kind,
          principal.TrustTier,
          principal.IsActive,
          principal.IsQuarantined,
          [.. roles]);
      }

      return new Response(items.Length, items);
    }
  }
}
