#region Purpose
// The YARP ingress: the yarp project with pinned ports in run mode, or the AddYarp image with its
// per-target exposure and generated web routes in publish mode.
#endregion

#region Design
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
// (appsettings.Development.json), the same ports as the yarp project's launch profile. Run mode
// hosts that project, so the pins and the profile are one process; do not also dotnet-run the yarp
// project beside dev run. The *.timewarp.work share path (Caddy in WSL, task 112) targets the http
// endpoint 63620, which run mode binds in-process so Caddy's X-Forwarded-Proto reaches the
// trusted-proxy middleware (task 305).
// The pins are the only port source in run mode (task 319): the run-mode yarp endpoints take the
// pin or, when it does not parse, no port, overriding the launch profile's applicationUrl. Closed-box
// test AppHosts clear both (`--Ingress:Port=` `--Ingress:HttpPort=`, via the tests' ClosedBoxAppHost
// helper) so they never bind the `dev run` ports and never reach a running dev instance. With no
// http pin the http endpoint stays proxied on a dynamic port; the in-process http binding (and
// with it Caddy's X-Forwarded-Proto) is a `dev run` behaviour, and the ingress smoke drives the
// forwarded headers itself from a loopback (trusted) client.
// Ingress:PublicUrl (unset by default; personal value belongs in user secrets, never committed —
// this repo dogfoods as the template) adds a "public" display URL on the ingress resource so the
// dashboard links to the externally shared hostname (e.g. https://arch.timewarp.work).
// Authentication:Entra:PublicOrigin is Web.Server config and is not copied from Ingress:PublicUrl.
// The dashboard display URL and the OIDC callback origin can differ (https://localhost:63610 vs
// the shared hostname); auto-copying would break one of those paths. Set PublicOrigin explicitly
// on Web.Server for any proxied Entra deployment.
// Public-host forwarding (task 070-008, task 305): YARP sends the DESTINATION host as Host on every
// route (its default) and the Web.Server routes chain WithTransformXForwarded(Set), which OVERWRITES
// X-Forwarded-Proto and X-Forwarded-Host with Request.Scheme and Request.Host. Run mode executes the
// yarp project, whose UseTrustedProxyForwardedHeaders applies those two headers only from a trusted
// proxy (loopback by default — Caddy on this machine) before that Set, so Web.Server reads the
// client-facing scheme and host. A peer that is not a trusted proxy cannot inject either header.
// Publish mode keeps the AddYarp image, which has no such middleware. Behind a TLS-terminating
// edge (ACA managed ingress, a Kubernetes controller) it may forward http; set SiteMetadata:BaseUrl
// and PublicOrigin on Web.Server there (not verified on ACA). One rule on every target:
// a public Host on the web hop breaks Azure Container Apps, whose internal ingress routes
// app-to-app traffic by Host. Web.Server's per-request WebAuthn RP-ID selection reads the public
// host from X-Forwarded-Host (HttpRequestHostAccessor only — no UseForwardedHeaders, which would also
// rewrite scheme and remote IP). The forwarded host only SELECTS among pre-approved RP IDs
// (WebAuthnOptions.AllowedRpIds), so a forged value can never mint a credential for an RP ID the
// operator did not approve; where web-server is directly reachable (run mode) a client can send its
// own X-Forwarded-Host exactly as it can send its own Host, and selection-only is what makes both
// safe — full argument in HttpRequestHostAccessor's Design region. Applied to Web.Server routes
// only; the api/grpc backends do no host-based selection and keep YARP's defaults.
// Task 393: a deployed target's public host is on WebAuthnOptions:AllowedRpIds. Azure Container
// Apps uses ingress.<environment default domain> (GetOutput of
// AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN, not the ingress endpoint, so web-server and
// ingress do not cycle). Compose and Kubernetes serve localhost, already allowed.
// A custom domain is the optional passkey-rp-host parameter, emitted only when it is set and is
// not localhost. dev deploy copies that user secret into the aspire environment.
// Guarded by aspire-tests/ingress-smoke-tests.cs (tasks 117, 070-008, 305): request-level smokes
// through the ingress with a foreign Host on a web route. A loopback client is a trusted proxy, so
// its X-Forwarded-Host is honored; the untrusted-peer case is the standalone yarp suite.
// Ingress readiness (task 058-001): the yarp resource carries WithHttpHealthCheck so "Healthy"
// means the http endpoint answered, not merely that the process or container started.
// Probe path (task 377): IngressPaths.WebServerAlivePath, a literal ingress route (outranks the web
// catch-all) rewritten to web-server's /alive. Healthy still means "YARP routed a request to a live
// web-server", but a probe no longer prerenders the SPA shell (probing "/" every 30s created a
// TimeWarp.State prerender store per probe). web-server maps /alive in every environment; see
// MapDefaultEndpoints' Design region for why that exposure is safe. WithHttpHealthCheck emits no
// probe into publish output (no EndpointProbeAnnotation; Compose, Kubernetes and ACA emit none), so
// only the AppHost process ever runs it. Guarded by aspire-tests IngressHealthCheck_Given_.
// AddYarp registers no health check of its own; see the inline note at the call site.
// Ingress port (task 311): ingress-port is the Compose host port and the port `dev open` uses on
// every target (the kind port-forward included), so one number per app. Like the 288 parameters it
// has no value in code — appsettings.json commits it (the template's ingressPort symbol, a dotnet new
// port generator scoped to "ingress-port": ", gives each generated app its own). Unlike them it keeps publishValueAsDefault so
// the published .env carries the port, and that overload needs the value at declaration time, so
// IngressResource reads Parameters:ingress-port from configuration (env and user secrets still override)
// instead of a literal; a missing value fails publish rather than falling back to a shared port.
// dev-cli-tests checks the committed value is a port and that the AppHost declares no literal.
// Ingress decision (carried from retired 070-001) — option (a): the cluster's ingress controller
// forwards ALL traffic to the YARP ingress, which keeps the per-service routing described above. Chosen over
// (b), per-service controller routes from WebServerApiRoutePrefixes, because (b) would fork the
// routing table into a second implementation per target and lose YARP's X-Forwarded-Host overwrite
// and /grpc prefix strip, which controller annotations express differently per vendor; with (a) the
// routes are identical in run mode, Compose and Kubernetes. The chart carries one networking.k8s.io
// Ingress (cluster-ingress) whose default backend is YARP's http endpoint (TLS terminates at the
// controller) and whose class is the ingress-class parameter (default nginx). The controller itself
// is cluster infrastructure and is NOT installed by this chart (an app chart that installs a
// cluster-scoped controller collides with every other release); install one per cluster.
#endregion

namespace TimeWarp.Architecture.Aspire;

/// <summary>Declares the ingress resource and its routes to the declared project resources.</summary>
internal static class IngressResource
{
  /// <summary>Adds the ingress for the current execution mode and publish target.</summary>
  internal static void Add(IDistributedApplicationBuilder builder, PublishTarget publishTarget, AppHostProjects projects)
  {
    // Host-port pins are run-mode only: published, the ingress-port parameter owns the host port and
    // a pinned endpoint port would emit an invalid host:host:container mapping (task 070-003).
    bool isRunMode = builder.ExecutionContext.IsRunMode;
    IngressPins pins = new
    (
      isRunMode && int.TryParse(builder.Configuration["Ingress:Port"], out int httpsPort) ? httpsPort : null,
      isRunMode && int.TryParse(builder.Configuration["Ingress:HttpPort"], out int httpPort) ? httpPort : null,
      builder.Configuration["Ingress:PublicUrl"]
    );

    if (isRunMode)
    {
      AddRunModeProject(builder, projects, pins);
    }
    else
    {
      AddPublishedImage(builder, publishTarget, projects, pins);
    }
  }

  private static void AddRunModeProject(IDistributedApplicationBuilder builder, AppHostProjects projects, IngressPins pins)
  {
    // Task 305: run mode executes the yarp project so UseTrustedProxyForwardedHeaders is the
    // process behind the pinned http port. Publish mode keeps the AddYarp image so Compose,
    // Kubernetes and ACA manifests stay on that image. DisableForwardedHeaders stops Aspire
    // injecting ASPNETCORE_FORWARDEDHEADERS_ENABLED, which trusts every peer and does not
    // honor X-Forwarded-Host.
    IResourceBuilder<ProjectResource> yarpProject = builder
      .AddProject<Projects.yarp>(YarpResourceName, options => options.LaunchProfileName = "Yarp")
      .DisableForwardedHeaders();

    // The launch profile's applicationUrl also carries 63610/63620. Overwrite its ports with the
    // pins so configuration is the only source: an unset pin (closed-box tests clear both,
    // task 319) means Aspire assigns the port.
    yarpProject = yarpProject.WithEndpoint("https", endpoint => endpoint.Port = pins.HttpsPort);

    if (pins.HttpPort is not null)
    {
      // Bind the pinned http port in-process. A DCP hop in front of it can replace
      // X-Forwarded-Proto with the scheme that hop saw (plain http) and drop Caddy's https
      // before UseTrustedProxyForwardedHeaders runs.
      yarpProject = yarpProject.WithEndpoint
      (
        "http",
        endpoint =>
        {
          endpoint.Port = pins.HttpPort.Value;
          endpoint.IsProxied = false;
        }
      );
    }
    else
    {
      yarpProject = yarpProject.WithEndpoint("http", endpoint => endpoint.Port = null);
    }

    if (!string.IsNullOrWhiteSpace(pins.PublicUrl))
    {
      // Display text IS the URL: a friendly label renders INSTEAD of the address in the
      // dashboard's URL column, hiding the hostname the link exists to surface.
      yarpProject = yarpProject.WithUrl(pins.PublicUrl, pins.PublicUrl);
    }

#if api
    yarpProject = yarpProject.WithReference(projects.ApiServer);
#endif
#if grpc
    yarpProject = yarpProject.WithReference(projects.GrpcServer);
#endif
#if web
    // Routes live in the yarp project's Development configuration (the same Set transform and
    // http://_http.web-server cluster the publish path builds below). WithReference injects
    // services__web-server__* so that address resolves. The http health check probes the
    // WebAliveRoute (IngressPaths.WebServerAlivePath -> web-server /alive), not the SPA shell;
    // with web excluded that route does not exist, so neither does the check.
    yarpProject = yarpProject.WithReference(projects.WebServer);
    yarpProject = yarpProject.WithHttpHealthCheck(global::TimeWarp.Foundation.Configuration.IngressPaths.WebServerAlivePath, endpointName: "http");

    // Task 335-002: the browser loads the SPA from the ingress, so the WASM exporter's span posts
    // carry the ingress origin. WithTimeWarpStateDevTools allows only web-server's own endpoints;
    // add the ingress https and http origins (and the public display URL when set) to the
    // inspector's CORS list. projects.StateDevTools is always set here (run mode, web on).
    if (projects.StateDevTools is not null)
    {
      ReferenceExpression ingressOrigins = string.IsNullOrWhiteSpace(pins.PublicUrl)
        ? ReferenceExpression.Create($"{yarpProject.GetEndpoint("https")},{yarpProject.GetEndpoint("http")}")
        : ReferenceExpression.Create($"{yarpProject.GetEndpoint("https")},{yarpProject.GetEndpoint("http")},{pins.PublicUrl}");
      projects.StateDevTools.WithEnvironment(StateDevToolsCorsOriginsEnvironmentVariable, ingressOrigins);
    }
#endif
  }

  private static void AddPublishedImage
  (
    IDistributedApplicationBuilder builder,
    PublishTarget publishTarget,
    AppHostProjects projects,
    IngressPins pins
  )
  {
    // Create the YARP resource
    IResourceBuilder<YarpResource> yarp = builder.AddYarp(YarpResourceName);

    // Task 070-003: published, the ingress is the ONLY service with a host port, pinned to the
    // ingress-port .env parameter (Compose would otherwise pick a random host port).
    if (publishTarget.Kubernetes is not null)
    {
      // Task 070-004 ingress decision (a): the cluster's ingress controller forwards everything to the
      // YARP ingress, which keeps doing the per-service routing below. The Ingress resource's default
      // backend is YARP's http endpoint (TLS terminates at the controller); the class is a parameter
      // because the controller belongs to the cluster, not to this chart.
      yarp = yarp.WithExternalHttpEndpoints();
      publishTarget.Kubernetes.AddIngress(KubernetesIngressResourceName)
        .WithIngressClass(builder.AddParameter(IngressClassParameterName, "nginx", publishValueAsDefault: true))
        .WithDefaultBackend(yarp.GetEndpoint("http"));
    }
    else if (publishTarget.ContainerApps is not null)
    {
      // Task 070-007: the ingress is the ONLY external container app (ACA's managed HTTPS ingress
      // with a public FQDN); web-server, api-server and grpc-server keep internal-only ingress.
      yarp = yarp.WithExternalHttpEndpoints();
      if (publishTarget.IsContainerAppsNeon)
      {
        yarp = yarp.PublishAsAzureContainerApp((_, app) => AcaNeonCompute.Apply(app));
      }
    }
    else
    {
      // Compose (the default target). Task 311: the value is the committed Parameters:ingress-port (see Design), read here only
      // because publishValueAsDefault needs it at declaration time to write it into the .env.
      string ingressPortValue = builder.Configuration[$"Parameters:{IngressPortParameterName}"]
        ?? throw new InvalidOperationException($"Parameters:{IngressPortParameterName} is not set; commit it in the AppHost appsettings.json.");
      IResourceBuilder<ParameterResource> ingressPort = builder.AddParameter(IngressPortParameterName, ingressPortValue, publishValueAsDefault: true);
      yarp = yarp
        .WithExternalHttpEndpoints()
        .PublishAsDockerComposeService((composeService, service) =>
          service.Ports = [.. service.Ports.Select(containerPort => $"{ingressPort.AsEnvironmentPlaceholder(composeService)}:{containerPort}")]);
    }

    if (!string.IsNullOrWhiteSpace(pins.PublicUrl))
    {
      // Display text IS the URL: a friendly label ("public") renders INSTEAD of the address in the
      // dashboard's URL column, hiding the very hostname the link exists to surface.
      yarp = yarp.WithUrl(pins.PublicUrl, pins.PublicUrl);
    }

#if web
    // The ingress forwards web routes over the HTTP endpoint — TLS terminates at the ingress edge
    // (and at Caddy on the public chain), not on this hop. This dates from 104-031's original-Host
    // forwarding (an https hop validated Web.Server's localhost dev cert against the PUBLIC
    // hostname and 502'd); task 070-008 sends the destination host instead but keeps the plain
    // HTTP hop on run, Compose and Kubernetes. api/grpc routes stay on their default (https) endpoints.
    // Under Publish:Target=aca the same named-endpoint address resolves to the https internal ingress
    // (Aspire's https upgrade), which works: Host is the destination (ACA routes and validates TLS by
    // it) and the public host travels in X-Forwarded-Host (task 070-008). No aca-specific web route.
    // NOTE (13.4.6): YarpCluster(EndpointReference) still emits the service-level address
    // "https+http://web-server", which service discovery resolves https-first — reintroducing the
    // mismatch. The explicit named-endpoint service-discovery address pins the scheme AND the
    // endpoint: http://_http.web-server resolves ONLY services__web-server__http__0.
    // The web routes below target the named-endpoint cluster (http://_http.web-server), which is
    // NOT a resource reference — this explicit reference keeps the services__web-server__* env
    // injected so the ingress can resolve that address.
    // Task 393: the browser's public host must be an allowed passkey RP ID on the first deploy.
    // ACA's FQDN is ingress.<environment domain>. Referencing the ingress endpoint would cycle
    // web-server and ingress; both sides reference the environment instead. localhost is already
    // in the C# default, so compose and kubernetes add a host only for a custom domain.
    string? passkeyRpHost = builder.Configuration[$"Parameters:{PasskeyRpHostParameterName}"];
    bool extraPasskeyHost = !string.IsNullOrWhiteSpace(passkeyRpHost)
      && !string.Equals(passkeyRpHost.Trim(), "localhost", StringComparison.OrdinalIgnoreCase);
    if (extraPasskeyHost)
    {
      projects.WebServer.WithEnvironment(
        "WebAuthnOptions__AllowedRpIds__0",
        builder.AddParameter(PasskeyRpHostParameterName, passkeyRpHost!.Trim(), publishValueAsDefault: true));
    }

    if (publishTarget.ContainerApps is not null)
    {
      // Public output on Aspire.Hosting.Azure.AppContainers 13.6.4. ingress.<domain> does not
      // reference the ingress endpoint (that would cycle web-server and ingress).
      global::Aspire.Hosting.Azure.BicepOutputReference defaultDomain = publishTarget.ContainerApps.GetOutput(
        "AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN");
      string allowedRpIdIndex = extraPasskeyHost ? "1" : "0";
      projects.WebServer.WithEnvironment(
        $"WebAuthnOptions__AllowedRpIds__{allowedRpIdIndex}",
        ReferenceExpression.Create($"{YarpResourceName}.{defaultDomain}"));
    }

    yarp = yarp.WithReference(projects.WebServer);

    // Readiness, not liveness (task 058-001): WITHOUT a health check the ingress reports Healthy
    // the moment DCP reports the YARP container Running, which is strictly weaker than "serves
    // traffic" — the DCP host-side proxy already accepts connections while Kestrel inside the
    // container is still starting, so a request issued in that window gets an immediate
    // connection EOF (HttpRequestException / "The response ended prematurely"), not an HTTP
    // status. Measured gap: up to ~810ms after WaitForResourceHealthyAsync("ingress") returned.
    // Every Aspire.Hosting.Testing suite that waits for "ingress" then sends a request raced
    // that window. This HTTP check probes the SAME host-side endpoint URL those tests use, so
    // Healthy now means "the proxy is wired AND YARP routed a request to a live web-server".
    // Path (task 377): IngressPaths.WebServerAlivePath, the literal health route added below,
    // rewritten to web-server's /alive — mapped in every environment and cheap, unlike "/" (the
    // SPA shell, a full prerender per probe) or "/health" (Development-only).
    // Web-gated: with web excluded there is no web-server, so the route and check are not emitted.
    // AppHost health checks run only while the AppHost runs the app (local dev + testing);
    // publish mode emits a manifest and never executes them, so deployments are unaffected.
    yarp = yarp.WithHttpHealthCheck(global::TimeWarp.Foundation.Configuration.IngressPaths.WebServerAlivePath, endpointName: "http");
#endif
    yarp.WithConfiguration(yarpConfiguration => ConfigureRoutes(yarpConfiguration, projects));
  }

  private static void ConfigureRoutes(IYarpConfigurationBuilder yarpConfiguration, AppHostProjects projects)
  {
#if api
    yarpConfiguration.AddRoute("/api/{**catch-all}", projects.ApiServer);
#endif
#if web
    global::Aspire.Hosting.Yarp.YarpCluster webServerHttp = yarpConfiguration.AddCluster("web-server-http", "http://_http.web-server");

    // Web.Server owns these top-level /api prefixes (generated: WebServerApiRoutePrefixes.All from
    // the web-contracts ApiRoute templates — see the Design region). Each literal segment outranks
    // the Api.Server catch-all above, so they win regardless of order.
    // ForwardPublicHost (task 070-008): Host stays the destination; X-Forwarded-Host is SET
    // (client value dropped) to the browser's public host, which HttpRequestHostAccessor reads for
    // passkey RP-ID selection against the allowlist. Web.Server routes ONLY — the api/grpc
    // backends keep YARP's defaults.
    static global::Aspire.Hosting.Yarp.YarpRoute ForwardPublicHost(global::Aspire.Hosting.Yarp.YarpRoute route) =>
      route.WithTransformXForwarded(xHost: global::Yarp.ReverseProxy.Transforms.ForwardedTransformActions.Set);

    foreach (string apiPrefix in global::WebServerApiRoutePrefixes.All)
    {
      ForwardPublicHost(yarpConfiguration.AddRoute($"/{apiPrefix}/{{**catch-all}}", webServerHttp));
    }

    // Tip discovery alias (104-020): exact bare /api → web so UseTipDiscoveryAlias can rewrite
    // to /api/tip. Without this, /api hits the Api.Server catch-all above. Not generated
    // (TWA0018 forbids bare `api` as a contracts prefix). One route: it also matches /api/, and
    // a second "/api/" route tied with it (500 AmbiguousMatchException, task 439).
    ForwardPublicHost(yarpConfiguration.AddRoute(global::TimeWarp.Foundation.Configuration.IngressPaths.TipDiscoveryAliasPath, webServerHttp));
    // Public readiness (task 439): the OpenAPI document advertises GET /api/health on this origin.
    ForwardPublicHost(yarpConfiguration.AddRoute(global::TimeWarp.Foundation.Configuration.IngressPaths.WebServerHealthPath, webServerHttp));

    // Ingress health route (task 377): the literal probe path outranks the catch-all below and is
    // rewritten to web-server's liveness endpoint. Mirrors the Development WebAliveRoute.
    yarpConfiguration.AddRoute(global::TimeWarp.Foundation.Configuration.IngressPaths.WebServerAlivePath, webServerHttp)
      .WithTransformPathSet(global::TimeWarp.Foundation.Configuration.HealthEndpointPaths.Alive);

#endif
#if grpc
    // The SPA's gRPC-Web client targets the ingress origin plus this prefix (task 312).
    yarpConfiguration.AddRoute(global::TimeWarp.Foundation.Configuration.IngressPaths.GrpcPathPrefix + "/{**catch-all}", projects.GrpcServer)
      .WithTransformPathRemovePrefix(global::TimeWarp.Foundation.Configuration.IngressPaths.GrpcPathPrefix);
#endif
#if web
    // Catch-all to Web.Server (SPA + everything not owned above): same public-host forwarding as
    // the literal /api routes so RP-ID selection sees the public host (task 070-008).
    ForwardPublicHost(yarpConfiguration.AddRoute(webServerHttp));
#endif
  }

  /// <summary>Ingress:Port / Ingress:HttpPort (run mode only) and the Ingress:PublicUrl display link.</summary>
  private sealed record IngressPins(int? HttpsPort, int? HttpPort, string? PublicUrl);
}
