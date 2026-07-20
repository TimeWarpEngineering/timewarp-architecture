#region Purpose
// Contract for obtaining a Passwordless.dev authentication token for a user.
#endregion

#region Design
// Token generation lives behind this endpoint because it requires the Passwordless API
// secret, which must never reach the browser; the Response carries only the opaque token.
// Not an IAuthApiRequest: it services the sign-in flow itself, before an authenticated
// user exists. GetMockResponseFactory returns a fixed token so SPA mock mode can exercise
// the flow without a Passwordless account.
// Task 110 round-1 review (M1, security): this is legacy Passwordless.dev infrastructure
// (dormant path — first-party WebAuthn/passkeys, task 104-003, is the going-forward sign-in
// story) whose handler mints a token for an arbitrary caller-supplied UserId with no
// server-side verification that the caller IS that user — an account-takeover primitive if
// reachable. Stamping it [EndpointAllowAnonymous] (110's first pass) was wrong: it blessed a
// live auth-bypass endpoint as intentionally public. Verified zero consumers — the SPA's
// PasswordlessService calls the Passwordless.dev SaaS (ApiUrl/create-token) directly, not this
// /api/signin-token route — so it is now [ClientOnlyContract]: no [ApiEndpoint], not generated,
// not reachable. The handler and the YARP-facing route template stay in place for 104-016/104-021
// to retire fully; do not re-add [ApiEndpoint]/[EndpointAllowAnonymous] to this contract without
// first closing the UserId-trust hazard.
#endregion

namespace TimeWarp.Architecture.Features.Auth;

public static partial class GetSignInToken
{
  [ApiRoute(RouteTemplate: "api/signin-token", HttpVerb.Get)]
  [ClientOnlyContract("Legacy Passwordless-era first-party sign-in token endpoint; unconsumed — the SPA uses the Passwordless SaaS directly, not this route. Retained client-only pending full retirement in 104-016/104-021; must NOT be a reachable anonymous server endpoint (mints tokens for arbitrary UserId with no proof of identity).")]
  public sealed partial class Query() : IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string UserId { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x.UserId).NotEmpty();
    }
  }
  public sealed class Response : BaseResponse
  {
    public string Token { get; }
    public Response(string token)
    {
      Token = token;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response("token");
  }
}
