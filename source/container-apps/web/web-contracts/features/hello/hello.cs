#region Purpose
// Minimal end-to-end sample contract: a GET greeting exercising the full Query/Validator/Response pattern.
#endregion

#region Design
// GET requests carry parameters in the query string, so the contract implements IQueryStringRouteProvider
// and owns URL construction — callers never hand-build routes.
// GetRoute() itself comes from the [ApiRoute] source generator; this file adds only query-string composition.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

[ApiEndpoint]
public static partial class Hello
{
  [ApiRoute(RouteTemplate: "api/Hello", HttpVerb.Get)]
  public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string Name { get; set; } = null!;

    public string GetRouteWithQueryString()
    {
      var parameters = new NameValueCollection { { nameof(Name), Name } };

      return $"{GetRoute()}?{this.GetQueryString(parameters)}";
    }
  }

  public class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(command => command.Name)
        .NotEmpty();
    }
  }

  public sealed class Response : BaseResponse
  {
    public string? Message { get; init; }
  }
}
