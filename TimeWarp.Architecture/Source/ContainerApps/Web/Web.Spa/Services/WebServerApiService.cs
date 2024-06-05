[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Testing.Common")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Api.Server.Integration.Tests")]
namespace TimeWarp.Architecture.Services;

/// <summary>
/// This is the Service that is used to interact with the Web.Server
/// </summary>
[ActivatorUtilitiesConstructor]
internal sealed class WebServerApiService : BaseAuthApiService, IWebServerApiService
{
  public WebServerApiService
  (
    IAccessTokenProvider accessTokenProvider,
    IHttpClientFactory httpClientFactory,
    IOptions<JsonSerializerOptions> options
  ) : base(accessTokenProvider, httpClientFactory, ServiceNames.WebServiceName, options) {}

  // add testing constructor
  public WebServerApiService
  (
    IAccessTokenProvider accessTokenProvider,
    HttpClient httpClient,
    JsonSerializerOptions jsonSerializerOptions
  ) : base(accessTokenProvider, httpClient, jsonSerializerOptions) {}

}

public interface IWebServerApiService : IApiService;
