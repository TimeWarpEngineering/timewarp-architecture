namespace TimeWarp.Architecture.Services;

/// <summary>
/// This is the Service that is used to interact with the API.Server
/// </summary>
public sealed class ApiServerApiService : BaseApiService, IApiServerApiService
{
  /// <summary>
  /// This is the Service that is used to interact with the API.Server
  /// </summary>
  [ActivatorUtilitiesConstructor]
  public ApiServerApiService(IHttpClientFactory httpClientFactory,
    IOptions<JsonSerializerOptions> options) : base(httpClientFactory, ServiceNames.ApiServiceName, options) {}

  /// <summary>
  /// Used for testing purposes
  /// </summary>
  /// <param name="httpClient"></param>
  /// <param name="jsonSerializerOptions"></param>
  public ApiServerApiService(HttpClient httpClient, JsonSerializerOptions jsonSerializerOptions) : base(httpClient, jsonSerializerOptions) {}

}

public interface IApiServerApiService : IApiService;
