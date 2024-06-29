namespace TimeWarp.Architecture.Features.Authentication;

public class AccountClaimsPrincipalFactoryWithRoles
(
  IAccessTokenProviderAccessor accessor,
  IStore Store,
  ISender Sender
) : AccountClaimsPrincipalFactory<RemoteUserAccount>(accessor)
{
  private AuthorizationState AuthorizationState => Store.GetState<AuthorizationState>();

  public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
  {
    ClaimsPrincipal claimsPrincipal = await base.CreateUserAsync(account, options);

    if (claimsPrincipal.Identity is not { IsAuthenticated: true }) return claimsPrincipal;

    var identity = (ClaimsIdentity)claimsPrincipal.Identity;
    if (identity.IsAuthenticated is false) return claimsPrincipal;

    await Sender.Send(new AuthorizationState.FetchCurrentUser.Action());
    if (AuthorizationState.Roles == null) return claimsPrincipal;

    foreach (Guid role in AuthorizationState.Roles)
    {
      identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
    }

    return claimsPrincipal;
  }
}
