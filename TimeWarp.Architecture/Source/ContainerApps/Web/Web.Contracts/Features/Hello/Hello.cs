namespace TimeWarp.Architecture.Features.Hellos;

public static partial class Hello
{
  [RouteMixin(RouteTemplate: "api/Hello", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string? Name { get; set; }
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
