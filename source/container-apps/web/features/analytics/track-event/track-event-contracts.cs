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
// CorrelationId is optional: the client (pipeline-behavior exemplar) supplies a per-session Guid
// when it has one; the server logs it as correlation.id and never requires it.
// [EndpointAllowAnonymous] (task 110): analytics ingestion — the payload carries only an event
// name, no PII, and pre-auth telemetry (page views before sign-in, etc.) is exactly the traffic
// this endpoint exists to capture; requiring auth would drop it.
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

[ApiEndpoint]
[EndpointAllowAnonymous("Analytics ingestion carries no PII and must capture pre-auth traffic (e.g. page views before sign-in).")]
public static partial class TrackEvent
{
  [ApiRoute("Analytics/TrackEvent", HttpVerb.Post)]
  public sealed partial class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {
    public string EventName { get; set; } = null!;
    public Guid? CorrelationId { get; set; }
  }

  public class Response : BaseResponse;

  public class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.EventName)
        .NotEmpty();
    }
  }
}
