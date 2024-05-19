namespace TimeWarp.Architecture.Services;

/// <summary>
/// This is the Service that is used to interact with the API.Server
/// </summary>
public sealed class ApiServerApiService
(
  IHttpClientFactory httpClientFactory,
  IOptions<JsonSerializerOptions> options
) : BaseApiService(httpClientFactory, Constants.ApiServiceName, options), IApiServerApiService;

public interface IApiServerApiService : IApiService;
