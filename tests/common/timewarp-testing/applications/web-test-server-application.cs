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
  internal const string WebHostUrl = "https://localhost:7000";
  internal const int WebPort = 7000;
  private const string ApiHostUrl = "https://localhost:7255";

  /// <param name="configureServices">
  /// Optional extras after the built-in test wiring (includes <see cref="MockAccessTokenProvider"/>).
  /// Used by C-create / <see cref="HostGraphFactory"/>.
  /// </param>
  public WebTestServerApplication(Action<IServiceCollection>? configureServices = null) :
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
          // Web.Server's OWN project directory carries appsettings (SampleOptions, WebAuthn, …).
          // Resolved via build-time metadata, not Assembly.Location — see ProjectContentRoot's
          // Design region (task 145-002 R2-1: Assembly.Location breaks for transitive consumers
          // that also reference Api.Server, since their flattened output dir collides).
          ContentRootPath = ProjectContentRoot.Resolve(
            typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly),
        },
        services =>
        {
          ConfigureServicesCallback(services);
          configureServices?.Invoke(services);
        }
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

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Web.Server.Program> webApplicationHost) =>
    new WebApiTestService(new TestApiService(HttpClient, ContractSerializationDefaults.Options));
}
