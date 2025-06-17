namespace TimeWarp.Architecture.Features.Authentication;

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
    if (identity.IsAuthenticated is false) return claimsPrincipal;

    await AuthorizationState.FetchCurrentUser();
    if (AuthorizationState.Roles == null) return claimsPrincipal;

    foreach (Guid role in AuthorizationState.Roles)
    {
      identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
    }

    return claimsPrincipal;
  }
}
