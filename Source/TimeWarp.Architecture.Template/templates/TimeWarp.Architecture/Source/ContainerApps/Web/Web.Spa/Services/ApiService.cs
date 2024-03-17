namespace TimeWarp.Architecture.Services;

[UsedImplicitly]
public sealed class ApiService
(
  IHttpClientFactory httpClientFactory,
  IOptions<JsonSerializerOptions> options
) : WebApiService(httpClientFactory, Constants.ApiServiceName, options);
