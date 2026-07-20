#region Purpose
// Contract for obtaining a Passwordless.dev authentication token for a user.
#endregion

#region Design
// Token generation lives behind this endpoint because it requires the Passwordless API
// secret, which must never reach the browser; the Response carries only the opaque token.
// Not an IAuthApiRequest: it services the sign-in flow itself, before an authenticated
// user exists. GetMockResponseFactory returns a fixed token so SPA mock mode can exercise
// the flow without a Passwordless account.
#endregion

namespace TimeWarp.Architecture.Features.Auth;

[ApiEndpoint]
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
