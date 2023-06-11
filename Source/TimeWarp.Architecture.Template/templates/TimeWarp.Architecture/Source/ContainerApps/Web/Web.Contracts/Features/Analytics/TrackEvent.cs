namespace TimeWarp.Architecture.Features.Analytics.Contracts;

public static partial class TrackEvent
{
  public sealed class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {
    public const string Route = "Analytics/TrackEvent";

    public string? EventName { get; set; }

    public HttpVerb GetHttpVerb() => HttpVerb.Post;
    public string GetRoute() => $"{Route}";
  }

  public record Response : BaseResponse {}

  public class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.EventName)
        .NotEmpty();
    }
  }
}

