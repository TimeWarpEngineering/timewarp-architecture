#region Purpose
// IApiService binding for the Api.Server backend, selected by named HttpClient.
#endregion

#region Design
// One concrete service (and marker interface) per backend lets callers pick the target server by
// injected interface rather than by URL, and lets DI configure each named HttpClient separately.
// The second constructor takes a raw HttpClient so tests can bypass IHttpClientFactory.
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>
/// This is the Service that is used to interact with the API.Server
/// </summary>
public sealed class ApiServerApiService : BaseApiService, IApiServerApiService
{
  /// <summary>
  /// This is the Service that is used to interact with the API.Server
  /// </summary>
  public ApiServerApiService
  (
    IHttpClientFactory httpClientFactory,
    IAccessTokenProvider accessTokenProvider,
    IOptions<JsonSerializerOptions> options
  ) : base(httpClientFactory, ServiceNames.ApiServiceName, accessTokenProvider, options) {}

  /// <summary>
  /// Used for testing purposes
  /// </summary>
  /// <param name="httpClient"></param>
  /// <param name="accessTokenProvider"></param>
  /// <param name="jsonSerializerOptions"></param>
  public ApiServerApiService
  (
    HttpClient httpClient,
    IAccessTokenProvider accessTokenProvider,
    JsonSerializerOptions jsonSerializerOptions
  ) : base(httpClient, accessTokenProvider, jsonSerializerOptions) {}

}

public interface IApiServerApiService : IApiService;
