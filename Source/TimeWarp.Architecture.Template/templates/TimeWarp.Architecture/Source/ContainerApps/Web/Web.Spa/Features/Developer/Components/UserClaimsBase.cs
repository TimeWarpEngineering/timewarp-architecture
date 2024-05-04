namespace TimeWarp.Architecture.Pages;

using System.Security.Claims;

/// <summary>
/// Base class for UserClaims component.
/// Retrieves claims present in the ID Token issued by Azure B2C.
/// </summary>
public class UserClaimsBase: ComponentBase
{
  [Inject]
  private AuthenticationStateProvider AuthenticationStateProvider { get; set;} = null!;

  protected string? AuthMessage;
  protected IEnumerable<Claim> Claims = Enumerable.Empty<Claim>();

  // Defines list of claim types that will be displayed after successfull sign-in.
  private readonly string[] ReturnClaims = { "idp", "name", "oid" };

  protected override async Task OnInitializedAsync()
  {
    await GetClaimsPrincipalData();
  }

  /// <summary>
  /// Retrieves user claims for the signed-in user.
  /// </summary>
  /// <returns></returns>
  private async Task GetClaimsPrincipalData()
  {
    // Gets an AuthenticationState that describes the current user.
    AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

    ClaimsPrincipal user = authState.User;

    // Checks if the user has been authenticated.
    if (user.Identity is { IsAuthenticated: true })
    {
      AuthMessage = $"{user.Identity.Name} is authenticated.";

      // Sets the claims value in _claims variable.
      // The claims mentioned in returnClaims variable are selected only.
      Claims = user.Claims;//.Where(x => ReturnClaims.Contains(x.Type));
    }
    else
    {
      AuthMessage = "The user is NOT authenticated.";
    }
  }
}
