namespace TimeWarp.Architecture.Testing;

// global:: — TimeWarp.Architecture.Yarp is this repo's gateway namespace, not the YARP package.
using global::Yarp.ReverseProxy.Configuration;

#region Purpose
// In-proc host for the standalone YARP gateway used by C-create HostGraphFactory.
#endregion

#region Design
// Development ReverseProxy config addresses Web.Server as http://_http.web-server (task 107).
// Aspire injects that named endpoint; this in-proc host has no DCP, so an IProxyConfigFilter
// rewrites the Web.Server cluster destination to WebTestServerApplication.WebHttpUrl
// (InProcTestPorts.WebHttpUrl) after LoadFromConfig. Generated LoadFromMemory routes still
// target the config cluster id "Web.Server" — that cross-provider merge is the path task 120
// smokes. AddConfigurationServiceEndpointProvider remains for any remaining service-name
// destinations (Api.Server / Grpc.Server).
// Api is optional: CreateWebYarpAsync boots Web+Yarp only; CreateWebApiYarpAsync still passes both.
// Listen URL is InProcTestPorts.YarpHostUrl (TIMEWARP_TEST_PORT_BASE; default :8443).
#endregion

/// <summary>
/// Used to launch the standalone YARP gateway.
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesCallback"/></remarks>
public class YarpTestServerApplication : TestServerApplication<Yarp.Server.Program>
{
#if(web)
  private readonly WebTestServerApplication WebTestServerApplication;
#endif
#if(api)
  private readonly ApiTestServerApplication? ApiTestServerApplication;
#endif
  internal static readonly int YarpPort = InProcTestPorts.YarpPort;

  /// <param name="configureServices">
  /// Optional extras after the built-in test wiring (C-create / <see cref="HostGraphFactory"/>).
  /// </param>
  public YarpTestServerApplication
  (
#if(web)
    WebTestServerApplication webTestServerApplication,
#endif
#if(api)
    ApiTestServerApplication? apiTestServerApplication = null,
#endif
    Action<IServiceCollection>? configureServices = null
  ) :
  base
  (
    new WebApplicationHost<Yarp.Server.Program>
    (
      urls:
      [
        InProcTestPorts.YarpHostUrl
      ],
      webApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Yarp.Server.IAssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
          // See ProjectContentRoot's Design region (task 145-002 R2-1) — resolves Yarp's own
          // project directory via build-time metadata instead of Assembly.Location, which
          // collides for consumers that also reference Web.Server / Api.Server.
          ContentRootPath = ProjectContentRoot.Resolve(
            typeof(TimeWarp.Architecture.Yarp.Server.IAssemblyMarker).Assembly),
        },
      services =>
      {
        ConfigureServicesCallback(services);
        configureServices?.Invoke(services);
      }
    )
  )
  {
#if(web)
    WebTestServerApplication = webTestServerApplication;
#endif
#if(api)
    ApiTestServerApplication = apiTestServerApplication;
#endif
  }

  protected static void ConfigureServicesCallback(IServiceCollection serviceCollection)
  {
    // Add configuration-based endpoint provider for test environment URLs
    // This allows us to map service names to literal URLs in appsettings.json
    serviceCollection.AddConfigurationServiceEndpointProvider();
#if(web)
    serviceCollection.AddSingleton<IProxyConfigFilter, YarpWebClusterHttpAddressFilter>();
#endif
  }

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Yarp.Server.Program> webApplicationHost) =>
    new WebApiTestService(new TestApiService(HttpClient, ContractSerializationDefaults.Options));
}

#if(web)
/// <summary>
/// Pins the config-defined Web.Server cluster onto the in-proc HTTP listener (task 120).
/// </summary>
internal sealed class YarpWebClusterHttpAddressFilter : IProxyConfigFilter
{
  public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancellationToken)
  {
    if (cluster.ClusterId != "Web.Server")
      return ValueTask.FromResult(cluster);

    Dictionary<string, DestinationConfig> destinations = new(StringComparer.OrdinalIgnoreCase)
    {
      ["Web.Server"] = new DestinationConfig { Address = WebTestServerApplication.WebHttpUrl }
    };

    return ValueTask.FromResult(cluster with { Destinations = destinations });
  }

  public ValueTask<RouteConfig> ConfigureRouteAsync
  (
    RouteConfig route,
    ClusterConfig? cluster,
    CancellationToken cancellationToken
  ) =>
    ValueTask.FromResult(route);
}
#endif
