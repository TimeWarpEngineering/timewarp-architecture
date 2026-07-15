#region Purpose
// The client-side abstraction every API handler calls to execute an IApiRequest.
#endregion

#region Design
// One generic method instead of per-endpoint methods: the request itself carries route and verb,
// so adding an endpoint never changes this interface.
// The tri-arm OneOf makes all outcomes explicit — typed DTO, file/stream download, or RFC 7807
// problem — so callers pattern-match instead of catching exceptions or sniffing content types.
// Abstracting the transport lets mock implementations stand in for real servers, enabling
// UX development against contracts alone.
// Terminal compositions for a mock decorator's inner IApiService:
//   NullApiService — no transport (mock-first SPA before a BFF); always 501 problem arm
//   WebServerApiService (or other HTTP binding) — real host fall-through when a BFF exists
// Product DI chooses the inner; Foundation does not auto-register NullApiService.
#endregion

namespace TimeWarp.Foundation;

public interface IApiService
{
  /// <summary>
  /// Get the response for the given request
  /// </summary>
  /// <typeparam name="TResponse"></typeparam>
  /// <param name="request"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>(IApiRequest request, CancellationToken cancellationToken) where TResponse : class;
}
