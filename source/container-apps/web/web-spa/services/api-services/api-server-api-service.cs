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
