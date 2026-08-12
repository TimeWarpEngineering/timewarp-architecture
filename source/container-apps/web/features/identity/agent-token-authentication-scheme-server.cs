#region Purpose
// Bearer-token AuthenticationHandler for the agent-token scheme: validates the Authorization header
// against IAgentTokenStore and populates HttpContext.User with the resulting claims.
#endregion

#region Design
// No header -> NoResult() (this scheme simply does not apply; lets another scheme, or an anonymous
// endpoint, proceed). A header present but not a well-formed "Bearer <token>" -> also NoResult()
// (standard ASP.NET Core convention for a scheme that does not recognize the credential shape it was
// handed — mirrors how the built-in bearer/JWT handlers behave). A well-formed Bearer header whose
// token IAgentTokenStore.Validate rejects (unknown/garbage/expired) -> Fail(): this scheme DID
// attempt to authenticate and failed.
// Quarantine mapping (task 104-004 §6, contrasted with issuance's 403 — see
// CompleteAgentTokenIssuance.Handler's Design region for the full rationale): here, at bearer
// VALIDATION time, an inactive/missing principal is a SILENT Fail() -> 401, the same generic
// unauthenticated outcome as an unknown or garbage token. This is deliberate, not an oversight: a
// caller presenting an old bearer token has proven nothing fresh (unlike a token-issuance ceremony,
// which requires a fresh signature over a one-time challenge) — a distinguishable 403 here would let
// a caller holding a merely-stolen/leaked token (no private key needed) learn "this principal is
// quarantined" without ever having proven possession of anything. The quarantine cutoff still takes
// effect immediately for every already-issued token — but THIS METHOD is what delivers that (the
// explicit PrincipalStore.GetPrincipalAsync + principal.IsActive check below, AFTER
// TokenStore.Validate succeeds), NOT IAgentTokenStore itself (round-1 finding M2 corrected a prior
// version of this paragraph and of IAgentTokenStore's own Design region, both of which wrongly
// attributed the principal re-read to Validate — that port has no IPrincipalStore access and never
// did; see IAgentTokenStore's Design region for the full, corrected division of responsibility). It
// just surfaces as "unauthenticated," not "forbidden," here.
// WWW-Authenticate/RFC 6750 §3.1: HandleChallengeAsync emits a bare "Bearer" challenge when no
// credential was ever presented (TokenWasPresented false), and "Bearer error=\"invalid_token\""
// when a Bearer credential WAS presented but rejected — RFC 6750 reserves the error/error_description
// parameters for the latter case, so a request with no Authorization header at all gets the bare
// challenge form. TokenWasPresented is instance (not static) state set during HandleAuthenticateAsync
// and read during HandleChallengeAsync — safe because ASP.NET Core's authentication middleware
// resolves a fresh handler instance per request via the scheme's handler factory.
// HandleForbiddenAsync (403, "insufficient_scope") is reached only when authentication SUCCEEDED but
// RequireClaim(ScopeClaimType, ...) failed — a valid, active-principal token that simply lacks the
// scope the endpoint's policy requires (see Agent_Protected_Endpoint_Tests's demo:invoke-only-token
// case for the exercised vector).
// Both challenge/forbidden responses write an RFC 7807 problem+json body via
// ContractSerializationDefaults — the same seam options every other identity response uses — rather
// than ASP.NET's bare-status-code default, so a bearer failure is machine-readable like every other
// identity error in this feature (task requirement: "Machine-readable errors").
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

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
    if (principal?.IsActive != true)
    {
      // Quarantine at validation time is a silent Fail -> 401 — see this class's Design region.
      return AuthenticateResult.Fail("Principal not found or inactive.");
    }

    List<Claim> claims = [new Claim(IdentitySessionDefaults.PrincipalIdClaimType, grant.PrincipalId.Value.ToString())];
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
