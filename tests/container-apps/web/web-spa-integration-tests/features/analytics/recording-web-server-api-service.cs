#region Purpose
// Records IWebServerApiService.GetResponse calls so analytics pipeline tests can assert POSTs.
#endregion

#region Design
// In-proc stand-in for the BFF client: no HTTP. Captures every request, optionally throws or
// returns SharedProblemDetails, otherwise a TrackEvent.Response. Singleton on the analytics
// test host; Reset() runs per fact so requests do not leak across tests.
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Analytics;

using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Features;
using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

internal sealed class RecordingWebServerApiService : IWebServerApiService
{
  public List<IApiRequest> Requests { get; } = [];
  public Exception? ExceptionToThrow { get; set; }
  public SharedProblemDetails? ProblemToReturn { get; set; }

  public void Reset()
  {
    Requests.Clear();
    ExceptionToThrow = null;
    ProblemToReturn = null;
  }

  public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    _ = cancellationToken;
    Requests.Add(request);

    if (ExceptionToThrow is not null)
    {
      throw ExceptionToThrow;
    }

    if (ProblemToReturn is not null)
    {
      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(ProblemToReturn);
    }

    if (new Response() is TResponse response)
    {
      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(response);
    }

    throw new InvalidOperationException($"No canned response for {typeof(TResponse)}.");
  }
}
