#region Purpose
// Enriches the OIDC ClaimsPrincipal with application role and permission claims at Entra sign-in.
#endregion

#region Design
// The identity provider's token carries no application roles/permissions; they live in the app's
// own backend (GetCurrentUser mock in the template), so they are pulled via
// AuthorizationState.FetchCurrentUser and added as ClaimTypes.Role + PermissionIds.ClaimType
// claims that SPA AddPermissionClaimPolicies match (task 182-003). Fetching inside the claims
// factory guarantees grants exist before any authorization policy evaluates.
#endregion

namespace TimeWarp.Architecture.Features.Authentication;

[CrossSliceReference(typeof(AuthorizationState), "Identity pipeline: claims factory enriches the signed-in principal with role/permission claims from AuthorizationState — authentication/authorization are deliberately coupled.")]
public class AccountClaimsPrincipalFactoryWithRoles : AccountClaimsPrincipalFactory<RemoteUserAccount>
{
  private readonly IStore Store1;
  public AccountClaimsPrincipalFactoryWithRoles
  (
    IAccessTokenProviderAccessor accessor,
    IStore Store
  ) : base(accessor)
  {
    Store1 = Store;
  }
  private AuthorizationState AuthorizationState => Store1.GetState<AuthorizationState>();

  public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
  {
    ClaimsPrincipal claimsPrincipal = await base.CreateUserAsync(account, options);

    if (claimsPrincipal.Identity is not { IsAuthenticated: true }) return claimsPrincipal;

    var identity = (ClaimsIdentity)claimsPrincipal.Identity;

    await AuthorizationState.FetchCurrentUser();
    if (AuthorizationState.Roles is { } roles)
    {
      foreach (Guid role in roles)
      {
        identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
      }
    }

    if (AuthorizationState.Permissions is { } permissions)
    {
      foreach (string permissionId in permissions)
      {
        identity.AddClaim(new Claim(PermissionIds.ClaimType, permissionId));
      }
    }

    return claimsPrincipal;
  }
}
