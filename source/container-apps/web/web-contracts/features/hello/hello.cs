#region Purpose
// Minimal end-to-end sample contract: a GET greeting exercising the full Query/Validator/Response pattern.
#endregion

#region Design
// GET requests carry parameters in the query string, so the contract implements IQueryStringRouteProvider
// and owns URL construction — callers never hand-build routes.
// GetRoute() itself comes from the [ApiRoute] source generator; this file adds only query-string composition.
// [EndpointAllowAnonymous] (task 110): a minimal end-to-end sample with no security surface — the
// template's own smoke-test endpoint, meant to be reachable with zero setup.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

[ApiEndpoint]
[EndpointAllowAnonymous("Minimal end-to-end sample endpoint; no security surface, deliberately reachable with zero setup.")]
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
