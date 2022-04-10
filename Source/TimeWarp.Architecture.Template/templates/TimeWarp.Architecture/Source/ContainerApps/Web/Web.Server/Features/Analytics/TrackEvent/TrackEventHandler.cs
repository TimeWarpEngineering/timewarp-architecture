namespace TimeWarp.Architecture.Features.Analytics;

public class TrackEventHandler : IRequestHandler<TrackEventRequest, TrackEventResponse>
{
  public Task<TrackEventResponse> Handle
  (
    TrackEventRequest aTrackEventRequest,
    CancellationToken aCcancellationToken
  )
  {
    // TODO implement code here that formats and sends data to your favorite Analytics tool

    var trackEventResponse = new TrackEventResponse(aTrackEventRequest.CorrelationId);
    return Task.FromResult(trackEventResponse);
  }
}
