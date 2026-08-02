namespace TimeWarp.Architecture.Testing;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public sealed class ApiTestServerApplication : TestServerApplication<Api.Server.Program>
{
  internal const string ApiHostUrl = "https://localhost:7255";
  internal const int ApiPort = 7255;

  /// <param name="configureServices">
  /// Optional extras after the built-in test wiring (C-create / <see cref="HostGraphFactory"/>).
  /// </param>
  public ApiTestServerApplication(Action<IServiceCollection>? configureServices = null) :
    base
    (
      new WebApplicationHost<Api.Server.Program>
      (
        urls:
        [
          ApiHostUrl
        ],
        webApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Api.Server.IAssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
          // See ProjectContentRoot's Design region (task 145-002 R2-1) — resolves Api.Server's
          // own project directory via build-time metadata instead of Assembly.Location, which
          // collides for consumers that also reference Web.Server.
          ContentRootPath = ProjectContentRoot.Resolve(
            typeof(TimeWarp.Architecture.Api.Server.IAssemblyMarker).Assembly),
        },
        services =>
        {
          ConfigureServicesCallback(services);
          configureServices?.Invoke(services);
        }
      )
    )
  { }

  private static void ConfigureServicesCallback(IServiceCollection serviceCollection)
  {
    Uri webServiceUri = ServiceUriHelper.GetServiceHttpsUri(ServiceNames.WebServiceName) ?? new Uri(ApiHostUrl);
    serviceCollection.AddHttpClient(ServiceNames.WebServiceName, client => client.BaseAddress = webServiceUri);
  }

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Api.Server.Program> webApplicationHost) =>
    new WebApiTestService(new TestApiService(HttpClient, ContractSerializationDefaults.Options));
}
