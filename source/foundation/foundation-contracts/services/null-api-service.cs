#region Purpose
// Terminal IApiService when no mock factory and no HTTP (or other) transport is registered.
#endregion

#region Design
// Null object for contract-first / mock-first SPAs before a BFF exists. Returns a 501
// SharedProblemDetails so callers keep pattern-matching OneOf instead of catching exceptions.
// Status 501 (Not Implemented) is the frozen platform semantic for "no backend for this call."
// Fixed Title/Detail — products that need custom copy write their own three-line type.
// Do not auto-register in DI; product composition chooses:
//   MockWebApiService → NullApiService   (mock-first, no transport)
//   MockWebApiService → WebServerApiService (mock + real host)
// Detail always includes the request type name; verb/route are best-effort so incomplete
// request types still return a problem arm instead of throwing inside this service.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// Terminal <see cref="IApiService"/> that always returns a 501 problem details arm.
/// Use as the inner service under a mock decorator when no real API transport exists.
/// </summary>
public sealed class NullApiService : IApiService
{
  private const string DefaultTitle = "No API backend";
  private const int NotImplementedStatus = (int)HttpStatusCode.NotImplemented;

  /// <inheritdoc />
  public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    ArgumentNullException.ThrowIfNull(request);

    SharedProblemDetails problem = new()
    {
      Title = DefaultTitle,
      Status = NotImplementedStatus,
      Detail = FormatDetail(request)
    };

    return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(problem);
  }

  private static string FormatDetail(IApiRequest request)
  {
    string typeName = request.GetType().FullName ?? request.GetType().Name;
    string verbAndRoute = TryFormatVerbAndRoute(request);
    return $"No mock factory and no API transport for {typeName} ({verbAndRoute}).";
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Detail formatting must not convert a missing transport into a thrown exception; incomplete route metadata falls back to a fixed phrase.")]
  private static string TryFormatVerbAndRoute(IApiRequest request)
  {
    try
    {
      return $"{request.GetHttpVerb()} {request.GetRoute()}";
    }
    catch (Exception)
    {
      return "route unavailable";
    }
  }
}
