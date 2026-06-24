namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.Http;
using Passwordless;
using TimeWarp.Architecture.Configuration;

/// <summary>
/// Used to launch the Web.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class WebTestServerApplication : TestServerApplication<Web.Server.Program>
{
  private const string WebHostUrl = "https://localhost:7000";
  private const string ApiHostUrl = "https://localhost:7255";

  public WebTestServerApplication() :
    base
    (
      new WebApplicationHost<Web.Server.Program>
      (
        urls:
        [
          WebHostUrl
        ],
        webApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
        },
        ConfigureServicesCallback
      )
    )
  { }

  protected static void ConfigureServicesCallback(IServiceCollection serviceCollection)
  {
    serviceCollection.PostConfigure<HttpClientFactoryOptions>
    (
      ServiceNames.WebServiceName,
      options => options.HttpClientActions.Add(client => client.BaseAddress ??= new Uri(WebHostUrl))
    );

    serviceCollection.PostConfigure<HttpClientFactoryOptions>
    (
      ServiceNames.ApiServiceName,
      options => options.HttpClientActions.Add(client => client.BaseAddress ??= new Uri(ApiHostUrl))
    );

    serviceCollection.AddSingleton<IAccessTokenProvider, MockAccessTokenProvider>();
  }

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Web.Server.Program> webApplicationHost)
  {
    IServiceProvider serviceProvider = webApplicationHost.ServiceProvider;

    IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    IAccessTokenProvider accessTokenProvider = serviceProvider.GetRequiredService<IAccessTokenProvider>();

    var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    IOptions<JsonSerializerOptions> jsonSerializerOptionsAccessor = Options.Create(jsonSerializerOptions);

    var webServerApiService = new WebServerApiService(accessTokenProvider, httpClientFactory, jsonSerializerOptionsAccessor);
    return new WebApiTestService(webServerApiService);
  }
}
