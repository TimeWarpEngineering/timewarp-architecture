namespace TimeWarp.Architecture.Services;

public class MockAuthenticationStateProvider : AuthenticationStateProvider
{
  public override Task<AuthenticationState> GetAuthenticationStateAsync()
  {
    var claims = new List<Claim>
    {
      new("oid", UserIds.SystemAdmin.ToString()),
      new(ClaimTypes.NameIdentifier, UserIds.SystemAdmin.ToString()),
      new(ClaimTypes.Name, "Mock User"),
      new(ClaimTypes.Role, RoleIds.Administrator.ToString()),
      new(ClaimTypes.Role, RoleIds.Developer.ToString()),
      new(ClaimTypes.Role, RoleIds.Accountant.ToString())
    };

    var identity = new ClaimsIdentity(claims, "Mock authentication type");
    var user = new ClaimsPrincipal(identity);
    var state = new AuthenticationState(user);
    return Task.FromResult(state);
  }
}
