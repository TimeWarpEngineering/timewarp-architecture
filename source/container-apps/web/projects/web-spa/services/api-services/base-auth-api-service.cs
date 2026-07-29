#region Purpose
// Base for authenticated API services (semantic subclass of BaseApiService).
#endregion

#region Design
// Kept as a distinct type so authenticated backends (WebServerApiService) stay clearly
// separated from anonymous BaseApiService compositions. Bearer acquisition is owned once by
// BaseApiService → HttpApiService (MSAL adapter on the acquire Func); this type no longer
// re-applies the token in GetResponse (that was a double RequestAccessToken per call).
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>
/// Base service for authenticated API.Server interaction via bearer token.
/// </summary>
internal abstract class BaseAuthApiService : BaseApiService
{
  protected BaseAuthApiService
  (
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    IAccessTokenProvider accessTokenProvider,
    IOptions<JsonSerializerOptions> options
  ) : base(httpClientFactory, httpClientName, accessTokenProvider, options)
  {
  }

  protected BaseAuthApiService
  (
    HttpClient httpClient,
    IAccessTokenProvider accessTokenProvider,
    JsonSerializerOptions jsonSerializerOptions
  ) : base(httpClient, accessTokenProvider, jsonSerializerOptions)
  {
  }
}
