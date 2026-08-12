#region Purpose
// Enriches the OIDC ClaimsPrincipal with application role claims fetched from the Web API at sign-in.
#endregion

#region Design
// The identity provider's token carries no application roles; they live in the app's own
// backend, so they are pulled via AuthorizationState.FetchCurrentUser and added as
// ClaimTypes.Role claims (role Guids as strings) that role-based policies match against.
// Fetching inside the claims factory guarantees role claims exist before any authorization
// policy evaluates.
#endregion

namespace TimeWarp.Architecture.Features.Authentication;

[CrossSliceReference(typeof(AuthorizationState), "Identity pipeline: claims factory enriches the signed-in principal with role claims from AuthorizationState — authentication/authorization are deliberately coupled.")]
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
    if (AuthorizationState.Roles == null) return claimsPrincipal;

    foreach (Guid role in AuthorizationState.Roles)
    {
      identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
    }

    return claimsPrincipal;
  }
}
