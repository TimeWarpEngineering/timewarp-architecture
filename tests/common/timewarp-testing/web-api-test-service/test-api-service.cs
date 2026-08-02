#region Purpose
// Self-contained IApiService over HttpClient for integration tests — no dependency on any UI client stack.
#endregion

#region Design
// Composes foundation HttpApiService so transport semantics (verb matrix, route/body, problem
// mapping) cannot drift from the SPA composer. timewarp-testing still cannot ProjectReference
// the SPA stack (flag-combination constraint); foundation-contracts is the shared WASM-safe home.
// Bearer is a fixed placeholder pinned on the client at construction (test hosts do not validate
// tokens); acquire Func is null. Pass null bearerToken for anonymous requests.
// GetHttpResponseMessage is public so WebApiTestService can assert on raw responses.
#endregion

namespace TimeWarp.Architecture.Testing;

using System.Net.Http.Headers;
using OneOf;

public sealed class TestApiService : IApiService
{
  private readonly HttpApiService Core;

  public TestApiService
  (
    HttpClient httpClient,
    JsonSerializerOptions jsonSerializerOptions,
    string? bearerToken = "dummy-token"
  )
  {
    if (bearerToken is not null)
    {
      httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(scheme: "Bearer", bearerToken);
    }

    Core = new HttpApiService(httpClient, jsonSerializerOptions, acquireBearerTokenAsync: null);
  }

  public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class =>
    Core.GetResponse<TResponse>(request, cancellationToken);

  /// <summary>Send the contract's request and return the raw response (for status/body assertions).</summary>
  public Task<HttpResponseMessage> GetHttpResponseMessage
  (
    IApiRequest apiRequest,
    CancellationToken cancellationToken
  ) =>
    Core.GetHttpResponseMessage(apiRequest, cancellationToken);
}
