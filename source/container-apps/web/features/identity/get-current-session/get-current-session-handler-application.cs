#region Purpose
// Server-side handler for the GetCurrentSession query: reads the ambient browser session, if any.
#endregion

#region Design
// IsAuthenticated and PrincipalId are always set together from the same IBrowserSessionService
// result, matching the contract Response's Design region (the pairing can never disagree).
// RoleIds (task 147-004): when authenticated, resolved via IEffectiveRolesResolver (empty store →
// Member; bootstrap may union Administrator). Unauthenticated → empty RoleIds list.
// Permissions (task 182-003): always via IPermissionEvaluator under the identity-session scheme
// (cookie sessions are human; agent-token never hits this handler). Unauthenticated → empty.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using TimeWarp.Architecture.Features;
using static TimeWarp.Architecture.Features.Identity.GetCurrentSession;

public sealed partial class GetCurrentSession
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IBrowserSessionService BrowserSessionService;
    private readonly IEffectiveRolesResolver EffectiveRolesResolver;
    private readonly IPermissionEvaluator PermissionEvaluator;

    public Handler(
      IBrowserSessionService browserSessionService,
      IEffectiveRolesResolver effectiveRolesResolver,
      IPermissionEvaluator permissionEvaluator)
    {
      BrowserSessionService = browserSessionService;
      EffectiveRolesResolver = effectiveRolesResolver;
      PermissionEvaluator = permissionEvaluator
        ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      PrincipalId? principalId = await BrowserSessionService.GetCurrentPrincipalIdAsync(cancellationToken);
      if (principalId is null)
      {
        return new Response(isAuthenticated: false, principalId: null, roleIds: [], permissions: []);
      }

      IReadOnlyList<Guid> roles = await EffectiveRolesResolver
        .GetEffectiveRoleIdsAsync(principalId.Value, cancellationToken)
        .ConfigureAwait(false);

      IReadOnlyList<string> permissions = await PermissionEvaluator
        .GetPermissionsAsync(
          principalId.Value,
          AuthenticationSchemeNames.IdentitySession,
          cancellationToken)
        .ConfigureAwait(false);

      return new Response(
        isAuthenticated: true,
        principalId,
        roleIds: [.. roles],
        permissions: [.. permissions]);
    }
  }
}
