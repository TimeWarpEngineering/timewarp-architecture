#region Purpose
// Contract for recording a named analytics event.
#endregion

#region Design
// [ApiEndpoint] opts the operation into FastEndpoint generation on web-server; [ApiRoute] owns the
// wire path so client and server cannot drift. Route string is the historical path without an
// `api/` prefix (Analytics/TrackEvent) — preserve exactly; do not "normalize" it.
// Response is an empty BaseResponse: the caller only needs success/problem typing for a
// fire-and-forget write. No MockResponseFactory, so in SPA mock mode this request falls through
// MockWebApiService to the real API service.
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

[ApiEndpoint]
public static partial class TrackEvent
{
  [ApiRoute("Analytics/TrackEvent", HttpVerb.Post)]
  public sealed partial class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {
    public string EventName { get; set; } = null!;
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
