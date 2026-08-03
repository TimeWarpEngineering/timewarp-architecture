#region Purpose
// AuthenticationHandler for closed-box mock principal (task 145-009): when Development/Testing +
// Authentication:UseMock and X-TimeWarp-Mock-Principal-Id is present, authenticates as an
// identity-session-compatible principal so scheme-restricted policies accept the request.
#endregion

#region Design
// AuthorizationMiddleware re-runs AuthenticateAsync for schemes listed on the policy and replaces
// HttpContext.User — so a bare middleware that only sets User is wiped by the identity-session
// cookie scheme's NoResult. This handler is a first-class named scheme always registered; it
// returns NoResult unless the fail-closed gate and header are satisfied, so Production and normal
// Development (no header) are unaffected. AuthenticationType is IdentitySessionDefaults.Scheme so
// assertion-based policies that key on that scheme continue to treat the principal as a browser
// session identity.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Services;

/// <summary>
/// Named authentication scheme for the fail-closed mock principal header.
/// </summary>
public sealed class MockIdentityPrincipalHandler
(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder,
  IHostEnvironment environment,
  IConfiguration configuration
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  /// <summary>Scheme name listed alongside identity-session on AuthenticatedPolicy.</summary>
  public const string SchemeName = "mock-identity-session";

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!MockAuthenticationDefaults.IsMockAuthActive
      (environment.EnvironmentName, configuration[MockAuthenticationDefaults.UseMockKey]))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    if (!Request.Headers.TryGetValue(MockAuthenticationDefaults.MockPrincipalIdHeader, out Microsoft.Extensions.Primitives.StringValues values)
      || !Guid.TryParse(values.ToString(), out Guid principalGuid))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    var claims = new List<Claim>
    {
      new(IdentitySessionDefaults.PrincipalIdClaimType, principalGuid.ToString()),
      new(ClaimTypes.Name, "mock-principal")
    };
    // AuthenticationType matches the browser session scheme so RequireAssertion policies that
    // check for identity-session still accept this principal.
    var identity = new ClaimsIdentity(claims, IdentitySessionDefaults.Scheme);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, SchemeName);
    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}
