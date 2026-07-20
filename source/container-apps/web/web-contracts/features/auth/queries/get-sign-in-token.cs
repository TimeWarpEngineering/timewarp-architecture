#region Purpose
// Contract for obtaining a Passwordless.dev authentication token for a user.
#endregion

#region Design
// Token generation lives behind this endpoint because it requires the Passwordless API
// secret, which must never reach the browser; the Response carries only the opaque token.
// Not an IAuthApiRequest: it services the sign-in flow itself, before an authenticated
// user exists. GetMockResponseFactory returns a fixed token so SPA mock mode can exercise
// the flow without a Passwordless account.
// [EndpointAllowAnonymous] (task 110): pre-auth sign-in ceremony by nature — there is no
// authenticated principal yet at the point this is called, so [EndpointAuthorize] is not an option.
// This is legacy Passwordless.dev infrastructure (dormant path — first-party WebAuthn/passkeys,
// task 104-003, is the going-forward sign-in story); the endpoint mints a token for an arbitrary
// caller-supplied UserId with no server-side verification that the caller IS that user — a known
// hazard, not newly introduced by this task, slated for retirement alongside the rest of the
// Passwordless path (104-016/104-021). Recorded here rather than silently left anonymous by
// omission.
#endregion

namespace TimeWarp.Architecture.Features.Auth;

[ApiEndpoint]
[EndpointAllowAnonymous("Pre-auth sign-in ceremony (legacy Passwordless.dev path, dormant since 104-003); no authenticated principal exists yet. Known UserId-trust hazard slated for retirement with the rest of the Passwordless path (104-016/104-021).")]
public static partial class GetSignInToken
{
  [ApiRoute(RouteTemplate: "api/signin-token", HttpVerb.Get)]
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
