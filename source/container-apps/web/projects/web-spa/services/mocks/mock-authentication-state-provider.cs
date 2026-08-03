#region Purpose
// AuthenticationStateProvider stand-in that reports a fixed signed-in admin user.
#endregion

#region Design
// Registered only via MockAuthenticationRegistration (Development/Testing + Authentication:UseMock).
// Role claims carry RoleIds (GUID strings), not role names, because authorization policies match
// on ids; the "oid" claim mirrors historical Entra claim shapes so claim-lookup code still works
// when UseEntra is later enabled. MockUserIds live under Features.Identity (task 104-021).
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>
/// Simulates an authenticated user for local development and authentication-dependent component testing.
/// </summary>
/// <remarks>
/// Use this provider in development builds when live authentication services such as Azure AD B2C are unavailable or impractical.
/// It returns a predictable principal with administrator, developer, and accountant roles so protected UI paths can be exercised offline.
/// </remarks>
public partial class MockAuthenticationStateProvider : AuthenticationStateProvider
{
  /// <summary>
  /// Gets a mock authentication state for the system administrator test user.
  /// </summary>
  /// <returns>An authentication state containing a claims principal with predefined development claims.</returns>
  public override Task<AuthenticationState> GetAuthenticationStateAsync()
  {
    List<Claim> claims = new()
    {
      new("oid", MockUserIds.SystemAdmin.ToString()),
      new(ClaimTypes.NameIdentifier, MockUserIds.SystemAdmin.ToString()),
      new(ClaimTypes.Name, "Mock User"),
      new(ClaimTypes.Role, RoleIds.Administrator.ToString()),
      new(ClaimTypes.Role, RoleIds.Developer.ToString()),
      new(ClaimTypes.Role, RoleIds.Accountant.ToString())
    };

    ClaimsIdentity identity = new(claims, "Mock authentication type");
    ClaimsPrincipal user = new(identity);
    AuthenticationState state = new(user);
    return Task.FromResult(state);
  }
}
