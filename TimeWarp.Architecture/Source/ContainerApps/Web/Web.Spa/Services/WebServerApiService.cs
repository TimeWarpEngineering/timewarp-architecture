[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Testing.Common")]
namespace TimeWarp.Architecture.Services;
internal sealed class WebServerApiService
(
  IAccessTokenProvider accessTokenProvider,
  IHttpClientFactory httpClientFactory,
  IOptions<JsonSerializerOptions> options
) : BaseAuthApiService(accessTokenProvider, httpClientFactory, ServiceNames.WebServiceName, options), IWebServerApiService;

public interface IWebServerApiService : IApiService;
