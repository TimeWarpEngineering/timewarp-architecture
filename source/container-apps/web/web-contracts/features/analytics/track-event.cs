#region Purpose
// Contract for recording a named analytics event.
#endregion

#region Design
// Implements IApiRequest by hand (const Route, GetHttpVerb/GetRoute) instead of the
// [ApiRoute] source-generation path — a worked example of the manual alternative when the
// generator is not wanted. Response is an empty BaseResponse: the caller only needs
// success/problem typing for a fire-and-forget write. No MockResponseFactory, so in SPA
// mock mode this request falls through MockWebApiService to the real API service.
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

public static partial class TrackEvent
{
  public sealed class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {
    public const string Route = "Analytics/TrackEvent";

    public string EventName { get; set; } = null!;

    public HttpVerb GetHttpVerb() => HttpVerb.Post;
    public string GetRoute() => $"{Route}";
  }

  public class Response : BaseResponse {}

  public class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.EventName)
        .NotEmpty();
    }
  }
}

