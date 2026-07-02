#region Purpose
// IApiService binding for the Web.Server (BFF) backend, selected by named HttpClient.
#endregion

#region Design
// internal + InternalsVisibleTo keeps app code depending only on IWebServerApiService, which is
// what allows MockWebApiService to decorate or replace it in DI; only tests construct it directly.
// The HttpClient constructor exists so tests can bypass IHttpClientFactory.
#endregion

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
