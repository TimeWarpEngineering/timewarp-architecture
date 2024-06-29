[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Testing.Common")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Api.Server.Integration.Tests")]
namespace TimeWarp.Architecture.Services;

/// <summary>
/// This is the Service that is used to interact with the Web.Server
/// </summary>
internal sealed class WebServerApiService : BaseAuthApiService, IWebServerApiService
{
  public WebServerApiService
  (
    IAccessTokenProvider accessTokenProvider,
    IHttpClientFactory httpClientFactory,
    IOptions<JsonSerializerOptions> options
  ) : base(httpClientFactory, ServiceNames.WebServiceName, accessTokenProvider, options) {}

  // add testing constructor
  public WebServerApiService
  (
    IAccessTokenProvider accessTokenProvider,
    HttpClient httpClient,
    JsonSerializerOptions jsonSerializerOptions
  ) : base(httpClient, accessTokenProvider, jsonSerializerOptions) {}

}

public interface IWebServerApiService : IApiService;
