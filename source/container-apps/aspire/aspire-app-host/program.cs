#region Purpose
// Composes the Aspire distributed application: service resources plus YARP ingress, gated by template feature flags.
#endregion

#region Design
// Preprocessor blocks mirror the dotnet-new template flags (api/grpc/web/yarp) so excluded services leave no trace.
// Resource names (see constants.cs) MUST equal ServiceNames.* in foundation-contracts — Aspire keys the
// injected services__{name}__https__0 env vars by resource name; server-side BaseAddress resolution breaks otherwise.
// webServer references itself so server-rendered (Auto) components can resolve their own API via service discovery.
// YARP literal /api routes owned by Web.Server beat the Api.Server catch-all by route precedence, not declaration order.
// The Web.Server route list below is hand-maintained and MUST gain a line whenever web-contracts adds a new
// top-level /api path segment — a missed entry sends the path to the Api.Server catch-all, which 404s with a bare
// body the SPA renders as a generic unhandled error (found the hard way with /api/identity in 104-003; candidate
// for generation from web-contracts ApiRoute templates per the prefer-analyzers directive).
// Ingress:Port pins the YARP host port so external clients and E2E tests get a stable ingress URL.
#endregion

namespace TimeWarp.Architecture.Aspire;

internal class Program
{
  private static void Main(string[] args)
  {
    IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

    // Declare project resources based on template flags
#if api
    // API Server is included in the template
    IResourceBuilder<ProjectResource> apiServer = builder
      .AddProject<Projects.api_server>(ApiServerProjectResourceName, options => options.LaunchProfileName = "Api.Server")
      .WithScalar();
#endif
#if grpc
    // gRPC Server is included in the template
    IResourceBuilder<ProjectResource> grpcServer = builder.AddProject<Projects.grpc_server>(GrpcServerProjectResourceName, options => options.LaunchProfileName = "Grpc.Server");
#endif
#if web
    // Web Server is included in the template
    IResourceBuilder<ProjectResource> webServer = builder.AddProject<Projects.web_server>(WebServerProjectResourceName, options => options.LaunchProfileName = "Web.Server")
      .WithExternalHttpEndpoints();

    // Add references to other services if they exist
#if api
    webServer = webServer.WithReference(apiServer);
#endif
#if grpc
    webServer = webServer.WithReference(grpcServer);
#endif
    // Self-reference for the web server
    webServer.WithReference(webServer);
#endif
#if yarp
    // YARP Reverse Proxy
    // YARP is included in the template
    int? ingressPort = int.TryParse(builder.Configuration["Ingress:Port"], out int port) ? port : null;

    // Create the YARP resource
    IResourceBuilder<YarpResource> yarp = builder.AddYarp(YarpResourceName);

    if (ingressPort is not null)
    {
      yarp = yarp.WithHostPort(ingressPort.Value);
    }

    yarp = yarp.WithConfiguration(yarpConfiguration =>
    {
#if api
      yarpConfiguration.AddRoute("/api/{**catch-all}", apiServer);
#endif
#if web
      // Web.Server owns these /api endpoints (see web-contracts ApiRoute templates); their
      // literal segments outrank the Api.Server catch-all above, so they win regardless of order.
      yarpConfiguration.AddRoute("/api/GetCurrentUser", webServer);
      yarpConfiguration.AddRoute("/api/Hello", webServer);
      yarpConfiguration.AddRoute("/api/Users/{**catch-all}", webServer);
      yarpConfiguration.AddRoute("/api/signin-token", webServer);
      yarpConfiguration.AddRoute("/api/identity/{**catch-all}", webServer);
#endif
#if grpc
      yarpConfiguration.AddRoute("/grpc/{**catch-all}", grpcServer)
        .WithTransformPathRemovePrefix("/grpc");
#endif
#if web
      yarpConfiguration.AddRoute(webServer);
#endif
    });
#endif

    builder.Build().Run();
  }
}