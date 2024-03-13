namespace TimeWarp.Architecture.Services;

[UsedImplicitly]
public sealed class ApiService
(
  IHttpClientFactory httpClientFactory,
  IOptions<JsonSerializerOptions> options
) : WebApiService(httpClientFactory.CreateClient(Constants.ApiServiceName), options);
