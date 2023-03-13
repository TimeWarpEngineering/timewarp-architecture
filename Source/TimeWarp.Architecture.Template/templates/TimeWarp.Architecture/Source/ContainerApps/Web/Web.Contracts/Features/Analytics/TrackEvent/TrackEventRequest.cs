namespace TimeWarp.Architecture.Features.Analytics;

[RouteMixin("Analytics/TrackEvent", HttpVerb.Post)]
public partial record TrackEventRequest : BaseRequest, IApiRequest, IRequest<TrackEventResponse>
{
  public string? EventName { get; set; }
}
