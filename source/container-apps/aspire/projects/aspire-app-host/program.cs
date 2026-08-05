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
// Schema evolution (task 147-007): committed EF migrations under platform/postgres/migrations/.
// AppHost AddEFMigrations (RunDatabaseUpdateOnStart + WaitFor migrations healthy + publish
// script/bundle) is the only runtime schema path under Aspire — web-server does not
// Migrate/EnsureCreated at startup. Same-project WaitForCompletion breaks DCP service-producer
// annotations under Aspire.Hosting.Testing after a successful ef-database-update; WaitFor uses the
// health check registered by RunDatabaseUpdateOnStart (package README).
// webServer references itself so server-rendered (Auto) components can resolve their own API via service discovery.
// YARP literal /api routes owned by Web.Server beat the Api.Server catch-all by route precedence, not declaration order.
// The Web.Server /api carve-outs are GENERATED, not hand-maintained (task 107): IngressRoutePrefixGenerator emits
// WebServerApiRoutePrefixes.All from the hosted web-contracts ApiRoute templates (outer ApiEndpoint + nested
// Query/Command route, minus ClientOnlyContract), collapsed to top-level api/<segment> prefixes; the loop below turns
// each into a /api/<segment>/{**catch-all} route. A new hosted web /api segment therefore appears in the ingress with
// no edit here, and a contract that stops being hosted (ClientOnlyContract) drops out automatically — the drift that
// shipped /api/identity and /api/Roles unreachable (104-003) is now impossible. TWA0017/TWA0018 fail the build on a
// prefix that would shadow another server's route space or that cannot be collapsed to a top-level segment.
// Task 104-020: exact /api and /api/ are hand-pinned to Web.Server for the tip discovery alias (web rewrites bare
// /api → /api/tip). Bare `api` cannot be a generated prefix (TWA0018); only the exact paths are pinned so
// /api/{**catch-all} still reaches Api.Server for non-web segments.
// Historical note: /api/signin-token was retired as a hosted endpoint (task 110 review M1) and so is absent from the
// generated set by construction, not by a hand-removed line.
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
// Guarded by aspire-tests/ingress-smoke-tests.cs (task 117): a request-level smoke through the
// ingress with a foreign Host header on a web route — the exact shape that 502'd when this hop
// ran over https — so a regression to the https cluster fails CI instead of shipping green.
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

    // Task 145-009 / dogfood: mock auth is OPT-IN only. Do not force Authentication:UseMock=true
    // for every Development AppHost run — that overrode appsettings (false) and turned on SPA/BFF
    // mock auth for local passkey dogfood. Forward the flag only when AppHost configuration
    // explicitly sets Authentication:UseMock (e.g. aspire-tests: --Authentication:UseMock=true).
    // Production never needs this injection; env vars still cannot enable mock outside
    // Development/Testing (handler/registration fail-closed gates).
    string? useMock = builder.Configuration["Authentication:UseMock"];
    if (string.Equals(useMock, "true", StringComparison.OrdinalIgnoreCase)
      || string.Equals(useMock, "1", StringComparison.OrdinalIgnoreCase))
    {
      webServer = webServer.WithEnvironment("Authentication__UseMock", "true");
    }

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

    // Task 147-007: first-class EF migrations resource (aspire.dev AddEFMigrations).
    // Migrations live in web-infrastructure (not an AppHost project resource — path form; typed
    // Projects.* is only generated for Aspire project resources).
    string webInfrastructureProject = Path.GetFullPath(
      Path.Combine(builder.AppHostDirectory, "../../../web/projects/web-infrastructure/web-infrastructure.csproj"));
    IResourceBuilder<EFMigrationResource> webMigrations = webServer
      .AddEFMigrations(WebMigrationsResourceName, "TimeWarp.Architecture.Persistence.PostgresDbContext")
      .WithMigrationsProject(webInfrastructureProject)
      .WithMigrationOutputDirectory("../../platform/postgres/migrations")
      .WithMigrationNamespace("TimeWarp.Architecture.Persistence.Migrations")
      .WithReference(postgresDb)
      .WaitFor(postgresDb)
      .RunDatabaseUpdateOnStart()
      .PublishAsMigrationScript()
      .PublishAsMigrationBundle();
    // Package README: RunDatabaseUpdateOnStart registers a health check so dependents can
    // WaitFor the migration resource until Active. (aspire.dev sample also shows WaitForCompletion
    // on the owning project — prefer WaitFor first: same-project WaitForCompletion under
    // Aspire.Hosting.Testing left web-server with invalid DCP service-producer annotations after
    // a successful ef-database-update.)
    webServer = webServer.WaitFor(webMigrations);
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
      // Display text IS the URL: a friendly label ("public") renders INSTEAD of the address in the
      // dashboard's URL column, hiding the very hostname the link exists to surface.
      yarp = yarp.WithUrl(ingressPublicUrl, ingressPublicUrl);
    }

#if web
    // Web routes preserve the ORIGINAL Host (per-request RP-ID selection). Outbound TLS from the
    // ingress would then validate Web.Server's localhost dev cert against the PUBLIC hostname
    // (Headers.Host overrides .NET's certificate-name validation target) and 502 with
    // RemoteCertificateNameMismatch. So the ingress forwards web routes over the HTTP endpoint —
    // TLS terminates at the ingress edge (and at Caddy on the public chain), not on this hop.
    // api/grpc routes do not preserve host and stay on their default (https) endpoints.
    // NOTE (13.4.6): YarpCluster(EndpointReference) still emits the service-level address
    // "https+http://web-server", which service discovery resolves https-first — reintroducing the
    // mismatch. The explicit named-endpoint service-discovery address pins the scheme AND the
    // endpoint: http://_http.web-server resolves ONLY services__web-server__http__0.
    // The web routes below target the named-endpoint cluster (http://_http.web-server), which is
    // NOT a resource reference — this explicit reference keeps the services__web-server__* env
    // injected so the ingress can resolve that address.
    yarp = yarp.WithReference(webServer);
#endif
    yarp = yarp.WithConfiguration(yarpConfiguration =>
    {
#if api
      yarpConfiguration.AddRoute("/api/{**catch-all}", apiServer);
#endif
#if web
      global::Aspire.Hosting.Yarp.YarpCluster webServerHttp = yarpConfiguration.AddCluster("web-server-http", "http://_http.web-server");

      // Web.Server owns these top-level /api prefixes (generated: WebServerApiRoutePrefixes.All from
      // the web-contracts ApiRoute templates — see the Design region). Each literal segment outranks
      // the Api.Server catch-all above, so they win regardless of order.
      // WithTransformUseOriginalHostHeader (task 104-031): forward the CLIENT's original Host header
      // to Web.Server instead of YARP rewriting it to the destination host, so HttpRequestHostAccessor
      // sees the public share hostname and the passkey RP-ID selection can match it against the
      // allowlist. Web.Server routes ONLY — the api/grpc backends keep YARP's default host rewrite.
      foreach (string apiPrefix in global::WebServerApiRoutePrefixes.All)
      {
        yarpConfiguration.AddRoute($"/{apiPrefix}/{{**catch-all}}", webServerHttp).WithTransformUseOriginalHostHeader(true);
      }

      // Tip discovery alias (104-020): exact bare /api → web so UseTipDiscoveryAlias can rewrite
      // to /api/tip. Without this, /api hits the Api.Server catch-all above. Not generated
      // (TWA0018 forbids bare `api` as a contracts prefix).
      yarpConfiguration.AddRoute("/api", webServerHttp).WithTransformUseOriginalHostHeader(true);
      yarpConfiguration.AddRoute("/api/", webServerHttp).WithTransformUseOriginalHostHeader(true);

#endif
#if grpc
      yarpConfiguration.AddRoute("/grpc/{**catch-all}", grpcServer)
        .WithTransformPathRemovePrefix("/grpc");
#endif
#if web
      // Catch-all to Web.Server (SPA + everything not owned above): same original-Host forwarding as
      // the literal /api routes so RP-ID selection sees the public host (task 104-031).
      yarpConfiguration.AddRoute(webServerHttp).WithTransformUseOriginalHostHeader(true);
#endif
    });
#endif

    builder.Build().Run();
  }
}