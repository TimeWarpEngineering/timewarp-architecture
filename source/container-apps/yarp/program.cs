#region Purpose
// Entry point for the YARP reverse-proxy gateway that fronts the container apps.
#endregion

#region Design
// Static routing (clusters + the Api.Server/Grpc.Server/Web.Server catch-alls) lives in the
// "ReverseProxy" configuration section, not code — operators can reshape it without a rebuild.
// The Web.Server /api carve-outs are the exception (task 107): they are GENERATED from the
// web-contracts ApiRoute templates (WebServerApiRoutePrefixes.All) and added as in-memory routes, so
// this gateway mirrors the Aspire AppHost ingress and cannot drift from the contracts. This closes
// the pre-existing gap where the standalone config did not carve /api/identity/** etc. out of the
// Api.Server catch-all (the 104-003 drift class). YARP merges config and in-memory providers, so the
// memory routes reference the config-defined "Web.Server" cluster directly (verified: a memory route
// resolves a cross-provider cluster). Each generated route preserves the client's original Host
// (RequestHeaderOriginalHost) for Web.Server's per-request passkey RP-ID selection (task 104-031),
// matching the config WebRoute.
// Task 104-020: exact /api and /api/ also pin to Web.Server so the tip discovery alias (bare API
// root → /api/tip rewrite on web-server) is reachable through ingress; without this, bare /api
// would hit Api.Server's /api/{**catch-all}. Not a generated prefix (TWA0018 forbids bare `api`).
// AddServiceDiscoveryDestinationResolver lets cluster destinations use Aspire logical service
// names instead of hard-coded addresses.
// Implements IAspNetProgram so every container app shares the same Configure* phase structure,
// keeping the template's entry points uniform; unused phases stay as empty methods rather than
// being removed.
#endregion

namespace TimeWarp.Architecture.Yarp.Server;
#if web
// global:: — inside namespace TimeWarp.Architecture.Yarp.Server, a bare "Yarp" binds to the app's
// own namespace, not the YARP package root.
using global::Yarp.ReverseProxy.Configuration;
#endif

public class Program : IAspNetProgram
{
  public static Task Main(string[] argumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(argumentArray);

    // This line should be sufficient for HTTPS configuration in Aspire with .NET 8
    builder.WebHost.UseKestrelHttpsConfiguration();

    builder.AddServiceDefaults();
    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    webApplication.MapDefaultEndpoints();

    ConfigureMiddleware(webApplication);
    ConfigureEndpoints(webApplication);

    return webApplication.RunAsync();
  }
  public static void ConfigureConfiguration(ConfigurationManager configurationManager) {}
  public static void ConfigureEndpoints(WebApplication webApplication) {}

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    webApplication.MapReverseProxy();
  }

  public static void ConfigureServices
  (
    IServiceCollection serviceCollection,
    IConfiguration configuration
  )
  {
    IReverseProxyBuilder reverseProxy = serviceCollection
      .AddReverseProxy()
      .LoadFromConfig(configuration.GetSection("ReverseProxy"));
#if web
    // Task 107: add the generated Web.Server /api carve-outs as in-memory routes (see Design region).
    // Each prefix becomes /{prefix}/{**catch-all} on the config-defined "Web.Server" cluster, with the
    // original-Host transform so passkey RP-ID selection sees the public host (task 104-031). Their
    // literal segments outrank the config "/api/{**catch-all}" -> Api.Server by route precedence.
    var originalHostTransform = new List<IReadOnlyDictionary<string, string>>
    {
      new Dictionary<string, string> { ["RequestHeaderOriginalHost"] = "true" },
    };

    var generatedWebRoutes = global::WebServerApiRoutePrefixes.All
      .Select(apiPrefix => new RouteConfig
      {
        RouteId = $"GeneratedWeb-{apiPrefix.Replace('/', '-')}",
        ClusterId = "Web.Server",
        Match = new RouteMatch { Path = $"/{apiPrefix}/{{**catch-all}}" },
        Transforms = originalHostTransform,
      })
      .ToList();

    // Tip discovery alias (104-020): exact bare /api → Web.Server (web rewrites to /api/tip).
    generatedWebRoutes.Add(new RouteConfig
    {
      RouteId = "TipDiscoveryAlias-api",
      ClusterId = "Web.Server",
      Match = new RouteMatch { Path = "/api" },
      Transforms = originalHostTransform,
    });
    generatedWebRoutes.Add(new RouteConfig
    {
      RouteId = "TipDiscoveryAlias-api-slash",
      ClusterId = "Web.Server",
      Match = new RouteMatch { Path = "/api/" },
      Transforms = originalHostTransform,
    });

    reverseProxy.LoadFromMemory(generatedWebRoutes, Array.Empty<ClusterConfig>());
#endif
    reverseProxy.AddServiceDiscoveryDestinationResolver();
  }
}
