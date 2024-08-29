namespace TimeWarp.Architecture.Features.Profiles;

public static partial class GetProfileData
{
  [RouteMixin(RouteTemplate: "api/Users/Current/Profile", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public string Alias { get; }
    public string Avatar { get; }

    public Response(string alias, string avatar)
    {
      Alias = Guard.Against.NullOrEmpty(alias);
      Avatar = Guard.Against.NullOrEmpty(avatar);
    }
  }
}
