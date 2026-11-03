#region Purpose
// Composes the Aspire distributed application: service resources plus YARP ingress, gated by template feature flags.
#endregion

#region Design
// Deploy guidance (operator map: target matrix, publish/deploy commands, production-safety rules, secrets,
// migrations per target, ingress topology, runtime neutrality) lives in skills/tw-deploy/SKILL.md; the
// Design regions of this project's files are its record. Keep the two in sync when publish-mode wiring changes. Deploying is
// operator-run with `dev deploy` / `dev deprovision` — never a CI step. `dev deploy --target <t>`
// publishes, deploys, migrates when the app has a database, smokes the ingress, and prints the URL.
// `dev deploy migrate` and `dev open` remain the one-step verbs (kind: `dev open` keeps a kubectl
// port-forward of the ingress controller). Both wrap the by-hand commands recorded here.
// Preprocessor blocks mirror the dotnet-new template flags (api/grpc/web/yarp/postgres) so excluded services leave no trace.
// Project resource names (see constants.cs) MUST equal ServiceNames.* in foundation-contracts — Aspire keys the
// injected services__{name}__{endpoint}__0 env vars by resource name, and Web.Server's https+http://{name} named
// clients resolve through them (task 312); a mismatch fails the first server-side call.
// Each concern lives in its own type, and its Design region holds that concern's reasoning:
//   PublishTarget (publish-target.cs): Publish:Target and the compute environment (Compose,
//     Kubernetes, aca, aca-neon), with each target's production-safety posture;
//   WebServerResource (web-server-resource.cs): run-mode exposure, State DevTools, published Entra
//     parameters, mock-auth opt-in;
//   PostgresResources (postgres-resources.cs): the database per target and web-migrations;
//   IngressResource (ingress-resource.cs): the YARP ingress, its ports, routes and health check;
//   AcaNeonCompute (aca-neon-compute.cs): aca-neon container-app scale;
//   SigNozDashboardLink (signoz-dashboard-link.cs): the optional SigNoz dashboard link.
// This file declares the project resources under their template flags, adds the references between
// them, and calls each concern in the order the model needs: web-server's own wiring and its api/grpc
// references come before Postgres, because AddEFMigrations snapshots web-server's environment onto
// ef-tool-web-migrations at the time it runs. AppHostProjects carries the declared projects to the
// concerns that wire across them; its members exist only under the same flags.
// webServer references itself so server-rendered (Auto) components can resolve their own API via service discovery.
// Container images (task 070-002): there are no hand-written Dockerfiles. `aspire publish` builds each
// AddProject resource's image with the .NET SDK container build (Microsoft.NET.Sdk.Web enables it by
// default). Run mode (task 305) hosts the yarp project so the trusted-proxy middleware is the process
// behind the pinned http port. Publish mode keeps the AddYarp container image. No ContainerRepository /
// ContainerFamily / ContainerBaseImage properties are set: Aspire supplies the repository and tag per
// resource, and the SDK picks the aspnet base image matching the project's TFM, so a .NET bump needs no
// image edit. Add those properties to a server csproj only when a deployment target actually needs a
// different base image (e.g. chiseled/alpine).
#endregion

namespace TimeWarp.Architecture.Aspire;

internal class Program
{
  private static void Main(string[] args)
  {
    IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
    SigNozDashboardLink.Add(builder);
    var publishTarget = PublishTarget.Declare(builder);

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
    IResourceBuilder<ProjectResource> webServer = builder.AddProject<Projects.web_server>(WebServerProjectResourceName, options => options.LaunchProfileName = "Web.Server");
    IResourceBuilder<ExecutableResource>? stateDevTools = WebServerResource.Configure(builder, webServer);

    // Add references to other services if they exist
#if api
    webServer.WithReference(apiServer);
#endif
#if grpc
    webServer.WithReference(grpcServer);
#endif
#if postgres
    // Inside the web block: Web.Server is Postgres' only consumer (see PostgresResources).
    PostgresResources.AddTo(builder, publishTarget, webServer);
#endif
    // Self-reference for the web server
    webServer.WithReference(webServer);
#endif
    AppHostProjects projects = new()
    {
#if api
      ApiServer = apiServer,
#endif
#if grpc
      GrpcServer = grpcServer,
#endif
#if web
      WebServer = webServer,
      StateDevTools = stateDevTools,
#endif
    };
#if yarp
    IngressResource.Add(builder, publishTarget, projects);
#endif
    AcaNeonCompute.ApplyTo(publishTarget, projects);
    builder.Build().Run();
  }
}
