namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.Http;
using TimeWarp.Architecture.Abuse;

/// <summary>
/// Used to launch the Web.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class WebTestServerApplication : TestServerApplication<Web.Server.Program>
{
  // Ports / URLs from InProcTestPorts (TIMEWARP_TEST_PORT_BASE; defaults 7000/7001/7255).
  // In-proc YARP Development cluster forwards Web.Server over http (task 107 https→http;
  // task 120 standalone smoke). Field aliases (not properties) avoid CA1056.
  internal static readonly string WebHostUrl = InProcTestPorts.WebHostUrl;
  internal static readonly string WebHttpUrl = InProcTestPorts.WebHttpUrl;
  internal static readonly int WebPort = InProcTestPorts.WebPort;
  internal static readonly int WebHttpPort = InProcTestPorts.WebHttpPort;
  private static readonly string ApiHostUrl = InProcTestPorts.ApiHostUrl;

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
          WebHostUrl,
          WebHttpUrl
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

    // Principal-registration abuse windows (task 104-015) are production-ish (~10/min/IP). Agent-key
    // and credential integration classes mint far more ceremonies than that per HostGraph lifetime,
    // so mid-suite requests became 429 (T0→T2 / TooManyRequests) — task 151. Integration hosts
    // disable the limiter by default; abuse-rate-limiting-tests.cs re-enables with tight limits via
    // HostGraphFactory configureWeb (PostConfigure after this callback, so it wins).
    serviceCollection.PostConfigure<AbuseRateLimitOptions>(options => options.Enabled = false);
  }

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Web.Server.Program> webApplicationHost) =>
    new WebApiTestService(new TestApiService(HttpClient, ContractSerializationDefaults.Options));
}
