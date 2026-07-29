#region Purpose
// SPA composition root for IApiService: HttpClient + MSAL token acquisition over HttpApiService.
#endregion

#region Design
// Thin composer over foundation HttpApiService — the shared transport (verb matrix, route/body
// prep, problem mapping, Stream→FileResponse) lives once in foundation-contracts so the test
// client cannot drift. This type only owns SPA concerns: named HttpClient creation and adapting
// IAccessTokenProvider into the per-request acquire Func. Token is requested per call so MSAL
// owns caching and refresh; a missing token degrades to anonymous so the server's 401 flows
// through SharedProblemDetails. Request/response JSON use the injected seam options
// (ContractSerializationDefaults).
#endregion

namespace TimeWarp.Architecture;

/// <summary>
/// Class that abstracts the WebAPI into a simple interface.
/// Given a Request return the Response.
/// </summary>
/// <remarks>
/// You don't care what http verb is used or even what protocol is used.
/// </remarks>
public abstract class BaseApiService : IApiService
{
  private readonly HttpApiService Core;

  /// <summary>
  /// This is the Service that is used to interact with the API.Server
  /// Given a Request return the Response.
  /// </summary>
  protected BaseApiService
  (
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    IAccessTokenProvider accessTokenProvider,
    IOptions<JsonSerializerOptions> jsonSerializerOptionsAccessor
  )
  {
    HttpClient httpClient = httpClientFactory.CreateClient(httpClientName);
    Core = new HttpApiService
    (
      httpClient,
      jsonSerializerOptionsAccessor.Value,
      CreateAcquireBearerToken(accessTokenProvider)
    );
  }

  /// <summary>
  /// This is the Service that is used to interact with the API.Server
  /// This constructor is provided for testing purposes only.
  /// </summary>
  protected BaseApiService
  (
    HttpClient httpClient,
    IAccessTokenProvider accessTokenProvider,
    JsonSerializerOptions jsonSerializerOptions
  )
  {
    Core = new HttpApiService
    (
      httpClient,
      jsonSerializerOptions,
      CreateAcquireBearerToken(accessTokenProvider)
    );
  }

  /// <inheritdoc />
  public virtual Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class =>
    Core.GetResponse<TResponse>(request, cancellationToken);

  private static Func<CancellationToken, Task<string?>> CreateAcquireBearerToken
  (
    IAccessTokenProvider accessTokenProvider
  ) =>
    async _ =>
    {
      AccessTokenResult tokenResult = await accessTokenProvider.RequestAccessToken().ConfigureAwait(false);
      return tokenResult.TryGetToken(out AccessToken? token) ? token.Value : null;
    };
}
