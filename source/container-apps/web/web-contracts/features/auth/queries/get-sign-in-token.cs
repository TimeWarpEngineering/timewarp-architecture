namespace TimeWarp.Architecture.Features.Auth;

public static partial class GetSignInToken
{
  [RouteMixin(RouteTemplate: "api/signin-token", HttpVerb.Get)]
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
