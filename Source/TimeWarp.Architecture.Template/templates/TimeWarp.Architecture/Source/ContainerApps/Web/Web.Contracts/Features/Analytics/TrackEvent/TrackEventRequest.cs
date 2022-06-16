namespace TimeWarp.Architecture.Features.Analytics;

using TimeWarp.Architecture.Features;

//[FeatureFlag(FeatureFlags.SegmentAnalytics)]
public record TrackEventRequest : BaseRequest, IApiRequest, IRequest<TrackEventResponse>
{
  public const string Route = "Analytics/TrackEvent";

  public string? EventName { get; set; }

  public HttpVerb GetHttpVerb() => HttpVerb.Post;
  public string GetRoute() => $"{Route}";
}
