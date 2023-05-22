namespace TimeWarp.Architecture.Features.Hello.Contracts;

public static partial class Hello
{
  public sealed record Query : BaseRequest, IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public const string Route = "Hello";

    public string? Name { get; set; }

    public HttpVerb GetHttpVerb() => HttpVerb.Get;
    public string GetRoute() => $"{Route}?{nameof(Name)}={Name}";
  }

  public sealed record Response : BaseResponse
  {
    public string? Message { get; set; }
  }

  public class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(command => command.Name)
        .NotEmpty();
    }
  }
}
