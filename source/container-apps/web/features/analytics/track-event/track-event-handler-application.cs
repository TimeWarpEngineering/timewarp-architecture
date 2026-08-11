#region Purpose
// Server-side extension point where template consumers forward TrackEvent data to their analytics provider.
#endregion

#region Design
// Deliberate no-op: the client-to-server tracking pipeline ships fully wired so only this body needs replacing.
// Always returns a success Response — analytics delivery failures must never surface to the user experience.
#endregion

namespace TimeWarp.Architecture.Features.Analytics.Application;

using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

public sealed partial class TrackEvent
{

  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    public Task<OneOf<Response, SharedProblemDetails>> Handle
    (
      Command command,
      CancellationToken cancellationToken
    )
    {
      // TODO implement code here that formats and sends data to your favorite Analytics tool

      var response = new Response();
      return Task.FromResult((OneOf<Response, SharedProblemDetails>)response);
    }
  }
}
