namespace TimeWarp.Architecture.Features.Analytics.Application;

using static TimeWarp.Architecture.Features.Analytics.Contracts.TrackEvent;

public sealed partial class TrackEvent
{

  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    public Task<OneOf<Response, SharedProblemDetails>> Handle
    (
      Command aTrackEventRequest,
      CancellationToken aCcancellationToken
    )
    {
      // TODO implement code here that formats and sends data to your favorite Analytics tool

      var response = new Response();
      return Task.FromResult((OneOf<Response, SharedProblemDetails>)response);
    }
  }
}
