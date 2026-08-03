#region Purpose
// Bearer-token AuthenticationHandler for the agent-token scheme on api-server: validates the
// Authorization header against IAgentTokenStore and populates HttpContext.User with claims.
#endregion

#region Design
// Behavior is intentional parity with web's AgentTokenAuthenticationHandler
// (web/features/identity/agent-token-authentication-scheme-server.cs) — see that file's Design
// region for full rationale (NoResult vs Fail, quarantine as silent 401, RFC 6750 WWW-Authenticate
// shapes, problem+json bodies via ContractSerializationDefaults).
// Callers of IAgentTokenStore.Validate MUST re-read the principal for liveness (IAgentTokenStore
// Design region) — this handler does that after a successful Validate, same as web.
// Instance TokenWasPresented is safe: ASP.NET Core resolves a fresh handler per request.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;

public sealed class AgentTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  private readonly IAgentTokenStore TokenStore;
  private readonly IPrincipalStore PrincipalStore;
  private bool TokenWasPresented;

  public AgentTokenAuthenticationHandler
  (
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAgentTokenStore tokenStore,
    IPrincipalStore principalStore
  ) : base(options, logger, encoder)
  {
    TokenStore = tokenStore;
    PrincipalStore = principalStore;
  }

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    string? authorizationHeader = Request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrEmpty(authorizationHeader))
    {
      return AuthenticateResult.NoResult();
    }

    if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out AuthenticationHeaderValue? headerValue)
      || !string.Equals(headerValue.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
      || string.IsNullOrEmpty(headerValue.Parameter))
    {
      return AuthenticateResult.NoResult();
    }

    TokenWasPresented = true;
    string token = headerValue.Parameter;

    AgentTokenGrant? grant = TokenStore.Validate(token);
    if (grant is null)
    {
      return AuthenticateResult.Fail("Invalid or expired token.");
    }

    Principal? principal = await PrincipalStore.GetPrincipalAsync(grant.PrincipalId, Context.RequestAborted);
    if (principal is null || !principal.IsActive)
    {
      // Quarantine at validation time is a silent Fail -> 401 — see this class's Design region.
      return AuthenticateResult.Fail("Principal not found or inactive.");
    }

    List<Claim> claims = [new Claim(AgentTokenDefaults.PrincipalIdClaimType, grant.PrincipalId.Value.ToString())];
    claims.AddRange(grant.Scopes.Select(scope => new Claim(AgentTokenDefaults.ScopeClaimType, scope)));

    var identity = new ClaimsIdentity(claims, AgentTokenDefaults.Scheme);
    var claimsPrincipal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(claimsPrincipal, AgentTokenDefaults.Scheme);

    return AuthenticateResult.Success(ticket);
  }

  protected override Task HandleChallengeAsync(AuthenticationProperties properties)
  {
    Response.StatusCode = StatusCodes.Status401Unauthorized;
    Response.Headers.WWWAuthenticate = TokenWasPresented ? "Bearer error=\"invalid_token\"" : "Bearer";
    Response.ContentType = "application/problem+json";

    var problemDetails = new SharedProblemDetails
    {
      Title = "Unauthorized",
      Status = StatusCodes.Status401Unauthorized,
      Detail = "A valid agent bearer token is required."
    };

    return Response.WriteAsync(JsonSerializer.Serialize(problemDetails, ContractSerializationDefaults.Options));
  }

  protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
  {
    Response.StatusCode = StatusCodes.Status403Forbidden;
    Response.Headers.WWWAuthenticate = "Bearer error=\"insufficient_scope\"";
    Response.ContentType = "application/problem+json";

    var problemDetails = new SharedProblemDetails
    {
      Title = "Forbidden",
      Status = StatusCodes.Status403Forbidden,
      Detail = "The token does not carry the required scope."
    };

    return Response.WriteAsync(JsonSerializer.Serialize(problemDetails, ContractSerializationDefaults.Options));
  }
}
