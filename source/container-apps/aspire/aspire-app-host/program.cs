#region Purpose
// Composes the Aspire distributed application: service resources plus YARP ingress, gated by template feature flags.
#endregion

#region Design
// Preprocessor blocks mirror the dotnet-new template flags (api/grpc/web/yarp/postgres) so excluded services leave no trace.
// Project resource names (see constants.cs) MUST equal ServiceNames.* in foundation-contracts — Aspire keys the
// injected services__{name}__https__0 env vars by resource name; server-side BaseAddress resolution breaks otherwise.
// Postgres is a container resource, not a project: its connection string is injected into Web.Server keyed by the
// DATABASE resource name (constants.cs PostgresDatabaseResourceName), and PostgresDbModule reads it by that same key.
// Only Web.Server references Postgres; Api.Server intentionally does not — so Postgres is declared INSIDE the web
// preprocessor block (the postgres directive nested within the web one), not gated on postgres alone: with web
// excluded it would otherwise be an unreferenced orphan container in the postgres-without-web combination.
// webServer references itself so server-rendered (Auto) components can resolve their own API via service discovery.
// YARP literal /api routes owned by Web.Server beat the Api.Server catch-all by route precedence, not declaration order.
// The Web.Server route list below is hand-maintained and MUST gain a line whenever web-contracts adds a new
// top-level /api path segment — a missed entry sends the path to the Api.Server catch-all, which 404s with a bare
// body the SPA renders as a generic unhandled error (found the hard way with /api/identity in 104-003; candidate
// for generation from web-contracts ApiRoute templates per the prefer-analyzers directive).
// Symmetrically, a line MUST be removed when its contract stops being a hosted endpoint (e.g. becomes
// [ClientOnlyContract]) — a stale route otherwise still forwards to Web.Server for a path FastEndpoints no
// longer serves. /api/signin-token was removed for this reason (task 110 round-1 review M1: the contract
// mints a Passwordless sign-in token for an arbitrary caller-supplied UserId with no proof of identity, has
// zero live consumers, and must not be a reachable server endpoint).
// Ingress:Port (https) / Ingress:HttpPort (http) pin the YARP host ports so external clients, E2E
// tests, and reverse proxies get stable ingress URLs. Development pins https 63610 / http 63620
// (appsettings.Development.json), matching the standalone yarp project's launchSettings on purpose:
// the two are alternative ingress modes that never run together, so "ingress https is 63610" holds
// in both. The *.timewarp.work share path (Caddy in WSL, task 112) targets the http endpoint 63620.
// Ingress:PublicUrl (unset by default; personal value belongs in user secrets, never committed —
// this repo dogfoods as the template) adds a "public" display URL on the ingress resource so the
// dashboard links to the externally shared hostname (e.g. https://arch.timewarp.work).
// Original-Host forwarding (task 104-031): the Web.Server routes chain
// WithTransformUseOriginalHostHeader(true) so the client's original Host header reaches Web.Server
// instead of being rewritten to the destination host — Web.Server's per-request WebAuthn RP-ID
// selection needs the PUBLIC host to match it against the allowlist. This is NOT UseForwardedHeaders
// and consumes no spoofable X-Forwarded-* header: the Host only SELECTS among pre-approved RP IDs
// (WebAuthnOptions.AllowedRpIds), so a forged Host can never mint a credential for an RP ID the
// operator did not approve. Applied to Web.Server routes only; the api/grpc backends do no host-based
// selection and keep YARP's default host rewrite.
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
#if postgres
    // Postgres is declared HERE, inside the web block, because Web.Server is its only consumer
    // (the api-server deliberately gets no reference). With web excluded there is nothing to
    // reference it, so it must not be declared at all — gating on the postgres flag alone would
    // boot an orphan container in the postgres-without-web template combination.
    // The data volume (dev-loop persistence) is config-gated: Postgres:UseDataVolume=false makes
    // the container ephemeral. Aspire test suites MUST pass that flag — the volume name is
    // deterministic per AppHost, so overlapping AppHost instances (dev run + test hosts, or
    // leaked test containers) sharing one volume corrupt postgres's WAL ("PANIC: could not
    // locate a valid checkpoint record"), after which every postgres start crash-loops and
    // WaitFor blocks forever. Found the hard way: PR 286 CI hang in web-spa-integration-tests.
    // The database resource name doubles as the ConnectionStrings key Aspire injects into
    // Web.Server (see constants.cs).
    bool usePostgresDataVolume = !string.Equals(
      builder.Configuration["Postgres:UseDataVolume"], "false", StringComparison.OrdinalIgnoreCase);

    IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres(PostgresResourceName);

    if (usePostgresDataVolume)
    {
      postgres = postgres.WithDataVolume();
    }

    IResourceBuilder<PostgresDatabaseResource> postgresDb = postgres.AddDatabase(PostgresDatabaseResourceName);
    webServer = webServer.WithReference(postgresDb).WaitFor(postgresDb);
#endif
    // Self-reference for the web server
    webServer.WithReference(webServer);
#endif
#if yarp
    // YARP Reverse Proxy
    // YARP is included in the template
    int? ingressHttpsPort = int.TryParse(builder.Configuration["Ingress:Port"], out int httpsPort) ? httpsPort : null;
    int? ingressHttpPort = int.TryParse(builder.Configuration["Ingress:HttpPort"], out int httpPort) ? httpPort : null;

    // Create the YARP resource
    IResourceBuilder<YarpResource> yarp = builder.AddYarp(YarpResourceName);

    if (ingressHttpsPort is not null)
    {
      yarp = yarp.WithEndpoint("https", endpoint => endpoint.Port = ingressHttpsPort.Value);
    }

    if (ingressHttpPort is not null)
    {
      yarp = yarp.WithEndpoint("http", endpoint => endpoint.Port = ingressHttpPort.Value);
    }

    string? ingressPublicUrl = builder.Configuration["Ingress:PublicUrl"];

    if (!string.IsNullOrWhiteSpace(ingressPublicUrl))
    {
      yarp = yarp.WithUrl(ingressPublicUrl, "public");
    }

#if web
    // Web routes preserve the ORIGINAL Host (per-request RP-ID selection). Outbound TLS from the
    // ingress would then validate Web.Server's localhost dev cert against the PUBLIC hostname
    // (Headers.Host overrides .NET's certificate-name validation target) and 502 with
    // RemoteCertificateNameMismatch. So the ingress forwards web routes over the HTTP endpoint —
    // TLS terminates at the ingress edge (and at Caddy on the public chain), not on this hop.
    // api/grpc routes do not preserve host and stay on their default (https) endpoints.
    EndpointReference webServerHttp = webServer.GetEndpoint("http");

#endif
    yarp = yarp.WithConfiguration(yarpConfiguration =>
    {
#if api
      yarpConfiguration.AddRoute("/api/{**catch-all}", apiServer);
#endif
#if web
      // Web.Server owns these /api endpoints (see web-contracts ApiRoute templates); their
      // literal segments outrank the Api.Server catch-all above, so they win regardless of order.
      // WithTransformUseOriginalHostHeader (task 104-031): forward the CLIENT's original Host header
      // to Web.Server instead of YARP rewriting it to the destination host, so
      // HttpRequestHostAccessor sees the public share hostname and the passkey RP-ID selection can
      // match it against the allowlist. Web.Server routes ONLY — the api/grpc backends do not select
      // an RP ID from the host and are left with YARP's default host rewrite.
      yarpConfiguration.AddRoute("/api/GetCurrentUser", webServer).WithTransformUseOriginalHostHeader(true);
      yarpConfiguration.AddRoute("/api/Hello", webServer).WithTransformUseOriginalHostHeader(true);
      yarpConfiguration.AddRoute("/api/Users/{**catch-all}", webServer).WithTransformUseOriginalHostHeader(true);
      yarpConfiguration.AddRoute("/api/identity/{**catch-all}", webServer).WithTransformUseOriginalHostHeader(true);
#endif
#if grpc
      yarpConfiguration.AddRoute("/grpc/{**catch-all}", grpcServer)
        .WithTransformPathRemovePrefix("/grpc");
#endif
#if web
      // Catch-all to Web.Server (SPA + everything not owned above): same original-Host forwarding as
      // the literal /api routes so RP-ID selection sees the public host (task 104-031).
      yarpConfiguration.AddRoute(webServer).WithTransformUseOriginalHostHeader(true);
#endif
    });
#endif

    builder.Build().Run();
  }
}