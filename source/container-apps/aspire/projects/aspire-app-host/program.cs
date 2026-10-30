#region Purpose
// Composes the Aspire distributed application: service resources plus YARP ingress, gated by template feature flags.
#endregion

#region Design
// Deploy guidance (operator map: target matrix, publish/deploy commands, production-safety rules, secrets,
// migrations per target, ingress topology, runtime neutrality) lives in skills/tw-deploy/SKILL.md; the
// reasoning below is its record. Keep the two in sync when publish-mode wiring changes. Deploying is
// operator-run with `dev deploy` / `dev deprovision` — never a CI step. `dev deploy --target <t>`
// publishes, deploys, migrates when the app has a database, smokes the ingress, and prints the URL.
// `dev deploy migrate` and `dev open` remain the one-step verbs (kind: `dev open` keeps a kubectl
// port-forward of the ingress controller). Both wrap the by-hand commands recorded here.
// Preprocessor blocks mirror the dotnet-new template flags (api/grpc/web/yarp/postgres) so excluded services leave no trace.
// Project resource names (see constants.cs) MUST equal ServiceNames.* in foundation-contracts — Aspire keys the
// injected services__{name}__{endpoint}__0 env vars by resource name, and Web.Server's https+http://{name} named
// clients resolve through them (task 312); a mismatch fails the first server-side call.
// Postgres is not a project: a container resource in run mode, Compose and Kubernetes, an Azure Database for
// PostgreSQL Flexible Server for aca. Its connection string is injected into Web.Server keyed by the
// DATABASE resource name (constants.cs PostgresDatabaseResourceName), and PostgresDbModule reads it by that same key.
// Only Web.Server references Postgres; Api.Server intentionally does not — so Postgres is declared INSIDE the web
// preprocessor block (the postgres directive nested within the web one), not gated on postgres alone: with web
// excluded it would otherwise be an unreferenced orphan container in the postgres-without-web combination.
// The Postgres data volume uses Aspire 13.6 WithVolume(env:) (task 266): same volume name and mount as
// WithDataVolume, plus POSTGRES_DATA_VOLUME (the mount path) on the container. What env: buys is small but
// real: the mount path is declared once in the model, visible in the dashboard and in published manifests,
// and follows the same convention the AppHost uses for projects/executables. It deliberately does NOT
// override PGDATA (the image's PGDATA lives under the mount; rebinding it to the mount root would
// re-initdb and orphan existing data). aspire-tests' postgres-volume-model-tests guard name/target parity.
// Postgres carries WithRepl (Aspire 13.6, task 262) only when the AppHost environment is Development: the
// dashboard psql REPL runs with the server's credentials, so it must never appear on a shared dashboard.
// Schema evolution (task 147-007, amended task 155): committed EF migrations under
// platform/postgres/migrations/. Migrations are explicit/on-demand in every environment: production
// applies them from the published PublishAsMigrationScript/PublishAsMigrationBundle pipeline
// artifacts (never AppHost auto-run); local/Aspire dev gets RunDatabaseUpdateOnStart as an
// out-of-box convenience (idempotent — cheap no-op on an already-current schema) plus the
// ef-database-update dashboard command on the web-migrations resource for an explicit re-run.
// web-server does not Migrate/EnsureCreated at startup, and — as of task 155, re-confirmed on
// Aspire 13.6 by task 270 — has NO wait edge on web-migrations at all (no WaitFor, no
// WaitForCompletion). Both wait forms were tried and both broke: WaitFor deadlocks any dashboard
// restart/rebuild of web-server after the first run because run-mode DefaultWaitBehavior only
// continues past Running, a state the once-run migration resource never re-enters (Finished is
// terminal, not Running). WaitForCompletion breaks DCP endpoint wiring for web-server. On
// 2026-08-05 (Aspire.Hosting.EntityFrameworkCore 13.4.6-preview.1.26319.6) the error was "Could
// not create Endpoint object(s): information about the port to expose the service is missing;
// service-producer annotation is invalid". Re-tested 2026-10-02 on Aspire 13.6.0 with
// Aspire.Hosting.EntityFrameworkCore 13.6.0-preview.1.26479.8 (task 270): the wait itself works
// (web-server logs "Waiting for resource 'web-migrations' to complete", then starts after
// "Successfully executed command 'ef-database-update'"), but the dotnet-ef tool resource inherits
// web-server's environment and DCP rejects it: "Could not perform substitution for environment
// variable ASPNETCORE_URLS ... service '/web-server-https' referenced by Executable
// '/ef-tool-web-migrations-…' specification is not produced by this Executable". web-server
// then listens on the Kestrel default http://localhost:5000 instead of its allocated port, the
// ingress health check times out, and aspire-tests fails 6 of 11 (11 of 11 without the edge).
// So the migration window stays; web-server is what tolerates it. SiteSettingsSeedHostedService
// asks EfPendingMigrationsProbe whether any migration this build was compiled with is missing
// from the database and waits before Kestrel starts (task 363; task 270 only checked that the
// site-settings table existed, so a new column on an existing table still crashed boot with
// 42703). A first run against an empty database, or a boot racing a new migration, logs no Error
// (guarded by aspire-tests' FirstRunOnEmptyDatabase fact, which boots an ephemeral Postgres, and
// web-server-integration-tests' boot-waits-for-pending-migrations regression). Restart
// never deadlocks because there is no edge to wait on, and Aspire.Hosting.Testing suites stay
// green.
// webServer references itself so server-rendered (Auto) components can resolve their own API via service discovery.
// TimeWarp.State DevTools (task 335-001): AddTimeWarpStateDevTools (package TimeWarp.State.DevTools.Hosting)
// adds the packaged inspector (TimeWarp.State.DevTools.App, a framework-dependent dll the package targets
// stamp onto this assembly) as the timewarp-state-devtools executable resource; the dashboard links it as
// "TimeWarp.State DevTools". WithTimeWarpStateDevTools sets TIMEWARP_STATE_DEVTOOLS_ENDPOINT on web-server
// and waits for the inspector; web-server (Development only) exports its State action spans there. Both
// calls sit in the IsRunMode block: the package already keeps the inspector out of the manifest, and the
// explicit gate keeps publish models from ever resolving the inspector path. compose/kubernetes/aca
// publish suites assert neither the resource nor the variable appears. Inside the web block: no
// web-server, no TimeWarp.State app to inspect. Server-side stores (prerender, InteractiveServer) export
// from web-server. Task 335-002: browser (WASM) stores export from web-spa (Debug builds, Development)
// to the inspector's POST /ingest/spans, which allows only listed CORS origins. WithTimeWarpStateDevTools
// lists web-server's endpoints; with yarp on, the run-mode ingress block adds the ingress origins through
// TimeWarp__State__DevTools__CorsOrigins on the inspector, because the browser loads the SPA from there.
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
// SigNoz (task 417): Observability:SigNoz:Url is operator configuration (AppHost user secrets or
// Observability__SigNoz__Url), never a committed template value, because this file ships in every
// generated app. An absolute https URL adds a run-mode external service named signoz. The dashboard
// link text is "SigNoz" and the target is that URL. Unset or blank adds nothing. Any other value
// throws at startup and names the key. AddExternalService (Aspire.Hosting 13.6.4) already excludes
// the resource from the manifest; the call is also IsRunMode-only, so publish and deploy models
// never contain it. There is no HTTP health check: the resource is a link, and a probe would mark
// it unhealthy when that host is unreachable from this machine. No telemetry is exported there.
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
// Container images (task 070-002): there are no hand-written Dockerfiles. `aspire publish` builds each
// AddProject resource's image with the .NET SDK container build (Microsoft.NET.Sdk.Web enables it by
// default). Run mode (task 305) hosts the yarp project so the trusted-proxy middleware is the process
// behind the pinned http port. Publish mode keeps the AddYarp container image. No ContainerRepository /
// ContainerFamily / ContainerBaseImage properties are set: Aspire supplies the repository and tag per
// resource, and the SDK picks the aspnet base image matching the project's TFM, so a .NET bump needs no
// image edit. Add those properties to a server csproj only when a deployment target actually needs a
// different base image (e.g. chiseled/alpine).
// Docker Compose publish target (task 070-003): AddDockerComposeEnvironment makes `aspire publish` emit
// docker-compose.yaml + .env (unfilled parameters) for standalone hardware; `aspire do prepare-compose`
// or `aspire deploy` fill the .env and build images through whichever container runtime Aspire detects
// (ASPIRE_CONTAINER_RUNTIME overrides). A compute environment only shapes publish/deploy, so run mode
// is unchanged; every publish-only branch below is IsPublishMode/IsRunMode-gated, never flag-gated, so
// every template flag combination publishes. Production-safety posture of the output:
//   - only the ingress has a host port (ingress-port parameter; the Ingress:Port/HttpPort pins are
//     run-mode only); web-server is external in run mode only, so a combination without the yarp
//     flag publishes no host port at all — the operator adds one for their own edge proxy, and the compose dashboard is disabled (it would publish an unauthenticated second port —
//     point OTEL_EXPORTER_OTLP_ENDPOINT at your own collector instead);
//   - secrets are .env parameters: postgres-password (AddPostgres' generated secret) and the Entra
//     settings (entra-client-secret is a secret parameter; Entra defaults to off). entra-enabled
//     is published as SignInOptions__IncludeMicrosoft365 (${ENTRA_ENABLED}), not a literal method
//     list. An unset client
//     secret publishes the placeholder "unused" on every publish target: the value overload of
//     AddParameter does not read configuration, and Azure Container Apps rejects an empty secret.
//     With Entra enabled and that placeholder still set, sign-in fails with AADSTS invalid_client;
//   - mock auth, browser-log forwarding and the Postgres REPL are never emitted: the UseMock forward
//     is run-mode only, and the other two are Development-only while publish runs as Production.
// aspire-tests' compose-publish-tests guard all of the above against the generated files.
// Postgres in Compose: fixed named volume postgres-data (the run-mode name hashes the AppHost path and
// would differ per checkout) and POSTGRES_DB creates the database on first initdb (run mode creates it
// through the AppHost, which a Compose stack does not have). Migrations run BY HAND from the published
// idempotent script (`dev deploy migrate` wraps it) — `docker compose exec -T postgres sh -c
// 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1' <
// efmigrations/web-migrations.sql` (the image enforces scram auth even on the in-container socket,
// and -T cannot prompt) — not a one-shot service: the bundle is a
// self-contained host binary with no image to run it in, and postgres has no host port for it to
// reach. Re-running the script is a no-op; web-server tolerates the not-yet-migrated window (above).
// Artifacts are CI-only, never committed: `dev publish compose` writes artifacts/aspire-output/compose
// (git-ignored) and workflow.yml uploads compose, .env and the SQL script (not the ~100 MB bundle).
// Kubernetes publish target (task 070-004): AddKubernetesEnvironment makes `aspire publish` emit a Helm
// chart; `aspire deploy` runs helm upgrade --install against the current kubectl context (Helm >= 4.2).
// Aspire assigns every compute resource to exactly ONE compute environment (a second environment
// without WithComputeEnvironment on every resource fails validation), so Compose and Kubernetes are
// alternatives selected by configuration: `aspire publish -- --Publish:Target=kubernetes` (default
// compose; any other value throws). The switch is read in publish mode only — run mode always
// declares the Compose environment, as before, and never sees Kubernetes resources or parameters.
// WithHelm takes the namespace and release name as parameters and the chart version as a parameter
// defaulting to 1.0.0. Images go to the registry from AddContainerRegistry (registry-endpoint +
// registry-repository parameters) attached with WithContainerRegistry — preview in Aspire 13.6, so
// ASPIRECOMPUTE003 is suppressed around those two calls only. Those four parameters carry no value
// in code (task 288): Aspire reads Parameters:<name> from configuration, and appsettings.json commits
// them — the app's kebab name for k8s-namespace / helm-release-name / registry-repository (the
// template's appNameKebab symbol rewrites it per generated app) and localhost:5001, the kind
// recipe's registry, for registry-endpoint. None is a secret; an env var or user secret overrides
// one per machine (AKS's registry). `dev deploy` lists them as its required kubernetes parameters
// and dev-cli-tests holds both agreements (value-less calls here, committed values there). No
// Kubernetes dashboard (a second workload with an unauthenticated UI).
// Ingress port (task 311): ingress-port is the Compose host port and the port `dev open` uses on
// every target (the kind port-forward included), so one number per app. Like the 288 parameters it
// has no value in code — appsettings.json commits it (the template's ingressPort symbol, a dotnet new
// port generator scoped to "ingress-port": ", gives each generated app its own). Unlike them it keeps publishValueAsDefault so
// the published .env carries the port, and that overload needs the value at declaration time, so
// program.cs reads Parameters:ingress-port from configuration (env and user secrets still override)
// instead of a literal; a missing value fails publish rather than falling back to a shared port.
// dev-cli-tests checks the committed value is a port and that program.cs declares no literal.
// Ingress decision (carried from retired 070-001) — option (a): the cluster's ingress controller
// forwards ALL traffic to the YARP ingress, which keeps the per-service routing above. Chosen over
// (b), per-service controller routes from WebServerApiRoutePrefixes, because (b) would fork the
// routing table into a second implementation per target and lose YARP's X-Forwarded-Host overwrite
// and /grpc prefix strip, which controller annotations express differently per vendor; with (a) the
// routes are identical in run mode, Compose and Kubernetes. The chart carries one networking.k8s.io
// Ingress (cluster-ingress) whose default backend is YARP's http endpoint (TLS terminates at the
// controller) and whose class is the ingress-class parameter (default nginx). The controller itself
// is cluster infrastructure and is NOT installed by this chart (an app chart that installs a
// cluster-scoped controller collides with every other release); install one per cluster.
// Production-safety posture of the chart: every Service is ClusterIP — the Ingress is the only way
// in, and without the yarp flag the chart has no Ingress at all; secrets (postgres password, Entra
// client secret, the derived connection strings) land in <resource>-secrets Secret objects backed by
// values.yaml `secrets.<resource>` with empty defaults, never in ConfigMaps; mock auth, browser-log
// forwarding and the REPL are absent for the same reasons as Compose. The postgres password appears
// under TWO values.yaml keys — secrets.postgres.postgres_password (postgres-secrets) and
// secrets.web_server.postgres_password (web-server-secrets: its connection strings) — which
// `aspire deploy` fills from the one parameter; a plain `helm install` must set both to the same
// value. ingress-class, postgres-storage-capacity and chart-version are publish/deploy-time
// parameters baked into the chart as literals (not .Values), so changing them means re-publishing
// or deploying with the parameter, not `helm --set`. `aspire deploy` takes the same switch:
// `aspire deploy -- --Publish:Target=kubernetes`. aspire-tests'
// kubernetes-publish-tests guard all of this against the generated chart.
// Postgres in Kubernetes: the published postgres-data volume binds by name to a Kubernetes persistent
// volume (PersistentVolumeClaim postgres-data, ReadWriteOnce, postgres-storage-capacity parameter,
// default 10Gi), which renders postgres as a single-replica StatefulSet with fsGroup set by Aspire.
// The password is AddPostgres' generated secret parameter. PublishAsKubernetesService is not used:
// the defaults (one replica, Aspire's fsGroup) are already right, and no probe is added.
// Migrations for Kubernetes: the published idempotent SQL script, run BY HAND once the StatefulSet is
// ready (`dev deploy migrate --target kubernetes` wraps it) — `kubectl exec -i -n <namespace>
// statefulset/postgres-statefulset -- sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d
// postgres-db -v ON_ERROR_STOP=1' < efmigrations/web-migrations.sql`. No migration Job: the bundle
// is a self-contained host binary with no image to run it in, and the bundle is not published for
// this target at all — publish output IS the chart directory, and Helm rejects any chart file over
// 5 MiB.
// `dev publish kubernetes` writes artifacts/aspire-output/kubernetes, gates it with the same suite and
// helm lint (when helm is on PATH); workflow.yml uploads the chart directory.
// Azure Container Apps publish target (task 070-007): `Publish:Target=aca` declares
// AddAzureContainerAppEnvironment("aca-env") and `aspire publish` emits Bicep (main.bicep plus one module
// per resource); `aspire deploy -- --Publish:Target=aca` provisions it with the Azure CLI credential. It
// is the Azure-only option beside AKS-through-Kubernetes (the portable default). Production-safety
// posture of the Bicep:
//   - WithDashboard(enable: false): otherwise Aspire adds an AspireDashboard dotNetComponents resource
//     on a public URL;
//   - only the YARP ingress container app is external (WithExternalHttpEndpoints in the aca branch);
//     web-server is external in run mode only, so web, api and grpc get internal ingress;
//   - Postgres is Azure Database for PostgreSQL Flexible Server (AddAzurePostgresFlexibleServer +
//     WithPasswordAuthentication), never a container app on an Azure Files share. Password auth (not
//     Entra auth) keeps PostgresDbModule's plain Npgsql connection string unchanged. The admin
//     password is the generated postgres-password @secure() parameter. Aspire stores the connection
//     string in a Key Vault (postgres-kv) and web-server reads it through a Key Vault-backed
//     container-app secret with its own managed identity — but Key Vault is NOT the only home of the
//     password: WithReference also gives web-server two plain container-app secrets built from that
//     parameter, postgres-db-password (POSTGRES_DB_PASSWORD) and postgres-db-uri (POSTGRES_DB_URI),
//     which web-server does not read. aca-publish-tests pins exactly that set;
//   - the server firewall is Aspire's AllowAllAzureIps rule (0.0.0.0–0.0.0.0) with public network
//     access on: it admits any Azure-hosted IP in ANY tenant, not just this environment's apps, so
//     the admin password is the barrier. VNet integration (private access) is the hardening step;
//     aca-publish-tests pins the rule set so a wider rule cannot land in code;
//   - Entra settings are the same parameters as Compose/Kubernetes (the client secret a secure
//     parameter → container-app secret; unset, the placeholder "unused" so the secret is non-empty);
//     mock auth, browser-log forwarding and the REPL are absent
//     for the same reasons as Compose.
// Flexible Server is declared in the aca branch only, so run mode — which never reads Publish:Target —
// keeps the AddPostgres container with its volume and REPL exactly as before (RunAsContainer would
// change the run-mode resource type and drop that wiring). Migrations for ACA: the published bundle,
// run BY HAND from the operator's machine (`dev deploy migrate --target aca`) with --connection through
// a temporary firewall rule for the operator's IP (the server admits Azure-hosted IPs only). The
// bundle needs no psql and no image; an ACA job would need one. The idempotent SQL script stays the
// psql alternative. Never EnsureCreated.
// HTTPS upgrade stays on (Aspire's default): with WithHttpsUpgrade(false) the internal endpoints
// become plain http, which ACA's internal ingress redirects to https (allowInsecure is false) — and
// YARP returns that redirect to the browser instead of following it.
// `dev publish aca` writes artifacts/aspire-output/aca and gates it with aspire-tests'
// aca-publish-tests (AcaPublish_Given_); publishing needs no Azure credentials, so CI runs it on every
// PR and uploads the Bicep and SQL script. Deploying is `dev deploy --target aca` (operator-run).
// Azure Container Apps with Neon (task 317): `Publish:Target=aca-neon` reuses this same
// AddAzureContainerAppEnvironment("aca-env") and the same external-ingress rule. It does not declare
// Flexible Server or postgres-kv. Postgres is two secret parameters the operator sets in AppHost user
// secrets (Parameters:neon-pooled-connection-string for web-server,
// Parameters:neon-direct-connection-string for the migration bundle). They are never forwarded on the
// aspire command line and never committed. PublishAsAzureContainerApp sets minReplicas 0, maxReplicas 1,
// 0.25 CPU and 0.5Gi on each container app so an idle app does not keep Neon or the consumption grant
// awake. web-server gets Postgres__RetryCount=6 and Postgres__RetryDelaySeconds=8. The delay is EF's
// maximum back-off, for the wake-up after Neon Free suspends (5 minutes, and that cannot be disabled).
// Those two environment callbacks, and the
// pooled WithReference, are attached to web-server AFTER AddEFMigrations. That call snapshots the
// startup project's environment callbacks onto ef-tool-web-migrations; a pooled reference in the
// snapshot collides with the migration resource's direct reference, because both emit
// ConnectionStrings__postgres-db and Aspire refuses two different connection-string resources on one
// variable. The connection timeout (Timeout=30) lives in the connection string. Run mode still ignores
// Publish:Target and keeps the AddPostgres container.
// Migrations are the published bundle on the direct connection (`dev deploy migrate --target aca-neon`);
// there is no firewall rule. `dev publish aca-neon` writes artifacts/aspire-output/aca-neon.
// workflow.yml uploads that Bicep and the SQL script beside the bundle, not the bundle. The
// environment's ACR Basic registry is not free. Task 400: WithContainerRegistry on each project
// (a ghcr.io ContainerRegistryResource) still publishes aca-env-acr at SKU Basic, and each
// container app's registries entry stays the ACR endpoint plus a managed identity. The environment
// property AzureContainerAppEnvironmentResource.ContainerRegistry throws for a non-ACR registry
// attached to the environment itself. The operator guide records both and the ACR Basic cost.
#endregion

namespace TimeWarp.Architecture.Aspire;

internal class Program
{
  private static void Main(string[] args)
  {
    IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
    AddSigNozDashboardLink(builder);

    // Task 070-004: Aspire assigns each compute resource to exactly one compute environment, so the
    // publish target is a configuration switch (Publish:Target = compose | kubernetes | aca | aca-neon), read in
    // publish mode only. Run mode always declares the Compose environment, exactly as before, and ignores it.
    string publishTarget = builder.ExecutionContext.IsPublishMode
      ? builder.Configuration[PublishTargetConfigurationKey] ?? ComposePublishTarget
      : ComposePublishTarget;
    IResourceBuilder<KubernetesEnvironmentResource>? kubernetes = null;
    IResourceBuilder<AzureContainerAppEnvironmentResource>? containerApps = null;

    if (string.Equals(publishTarget, KubernetesPublishTarget, StringComparison.OrdinalIgnoreCase))
    {
      // Task 070-004: `aspire publish` emits a Helm chart; `aspire deploy` runs helm upgrade --install
      // against the current kubectl context. Namespace, release name and chart version are parameters;
      // namespace, release name and the registry parameters below are value-less here and committed in
      // appsettings.json Parameters (see Design). No dashboard: it would be a second workload with an
      // unauthenticated UI beside the app.
      kubernetes = builder.AddKubernetesEnvironment(KubernetesEnvironmentResourceName)
        .WithHelm(helm => helm
          .WithNamespace(builder.AddParameter(KubernetesNamespaceParameterName))
          .WithReleaseName(builder.AddParameter(HelmReleaseNameParameterName))
          .WithChartVersion(builder.AddParameter(HelmChartVersionParameterName, "1.0.0", publishValueAsDefault: true)))
        .WithDashboard(enabled: false);

      // Images are pushed to the operator's registry (endpoint + repository parameters). Container
      // registries are preview in Aspire 13.6; suppressed here only, where the registry is attached.
#pragma warning disable ASPIRECOMPUTE003 // Preview API: AddContainerRegistry / WithContainerRegistry (Aspire 13.6).
      IResourceBuilder<ContainerRegistryResource> registry = builder.AddContainerRegistry(
        ContainerRegistryResourceName,
        builder.AddParameter(RegistryEndpointParameterName),
        builder.AddParameter(RegistryRepositoryParameterName));
      kubernetes = kubernetes.WithContainerRegistry(registry);
#pragma warning restore ASPIRECOMPUTE003
    }
    else if (string.Equals(publishTarget, ContainerAppsPublishTarget, StringComparison.OrdinalIgnoreCase))
    {
      // Task 070-007: `aspire publish` emits Bicep for an Azure Container Apps environment (with its own
      // container registry and Log Analytics workspace); `aspire deploy` provisions it in the operator's
      // subscription. No dashboard: Aspire deploys it as a public container app by default, an
      // unauthenticated-by-design second front door beside the ingress.
      containerApps = builder.AddAzureContainerAppEnvironment(ContainerAppsEnvironmentResourceName).WithDashboard(enable: false);
    }
    else if (string.Equals(publishTarget, ContainerAppsNeonPublishTarget, StringComparison.OrdinalIgnoreCase))
    {
      // Task 317: the same environment as aca. Postgres is wired below as Neon connection strings, not
      // a Flexible Server, so this branch only declares the environment. The container-app scale
      // (min replicas 0, small CPU and memory) is applied to each compute resource further down.
      containerApps = builder.AddAzureContainerAppEnvironment(ContainerAppsEnvironmentResourceName).WithDashboard(enable: false);
    }
    else if (string.Equals(publishTarget, ComposePublishTarget, StringComparison.OrdinalIgnoreCase))
    {
      // Task 070-003: `aspire publish` target for standalone hardware (compose.yaml + .env). A compute
      // environment only shapes publish/deploy output; run mode (`dev run`) ignores it. No compose
      // dashboard: it would publish a second, unauthenticated host port beside the ingress.
      builder.AddDockerComposeEnvironment(ComposeEnvironmentResourceName).WithDashboard(enabled: false);
    }
    else
    {
      throw new InvalidOperationException(
        $"{PublishTargetConfigurationKey} '{publishTarget}' is not a publish target; use '{ComposePublishTarget}', '{KubernetesPublishTarget}', '{ContainerAppsPublishTarget}' or '{ContainerAppsNeonPublishTarget}'.");
    }

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

    // External in run mode only (dashboard links straight to Web.Server). Published, the ingress is
    // the single host-exposed service; an external web-server would publish its own host port.
    // Task 335-002: null in publish mode; the ingress block adds its origins to the inspector's CORS list.
    IResourceBuilder<ExecutableResource>? stateDevTools = null;
    if (builder.ExecutionContext.IsRunMode)
    {
      webServer = webServer.WithExternalHttpEndpoints();

      // Task 335-001: the packaged TimeWarp.State DevTools inspector, run mode only (see Design).
      // web-server gets TIMEWARP_STATE_DEVTOOLS_ENDPOINT and waits for the inspector.
      stateDevTools = builder.AddTimeWarpStateDevTools();
      webServer = webServer.WithTimeWarpStateDevTools(stateDevTools);
    }

    // Task 070-003: published Entra settings are parameters, never literals. Defaults keep Entra off.
    // This block runs for every publish target, not only Azure Container Apps.
    // entra-enabled stays a deploy-time parameter. It is published as
    // SignInOptions__IncludeMicrosoft365, which asks web-server to append Microsoft365 to the
    // checked-in Methods list. The AppHost does not write SignInOptions__Methods__0, so a
    // checked-in Microsoft365-only list is not replaced with Passkey.
    // AddParameter(name, value, secret: true) ignores configuration (its value callback is constant).
    // Capture Parameters:entra-client-secret here. When it is unset, use "unused" (not a credential).
    // Azure Container Apps rejects an empty container-app secret; the same placeholder keeps the
    // other targets' secret non-empty. With Entra enabled and this placeholder still set, sign-in
    // fails with AADSTS invalid_client. aspire deploy runs as Production and does not load user
    // secrets; dev deploy copies a set user secret into the process environment.
    if (builder.ExecutionContext.IsPublishMode)
    {
      const string unsetEntraClientSecret = "unused";
      string? configuredEntraClientSecret = builder.Configuration[$"Parameters:{EntraClientSecretParameterName}"];
      string entraClientSecret = string.IsNullOrWhiteSpace(configuredEntraClientSecret)
        ? unsetEntraClientSecret
        : configuredEntraClientSecret;
      IResourceBuilder<ParameterResource> entraEnabledParameter =
        builder.AddParameter(EntraEnabledParameterName, "false", publishValueAsDefault: true);
      webServer = webServer
        .WithEnvironment("SignInOptions__IncludeMicrosoft365", entraEnabledParameter)
        // TenantId is the OIDC authority (organizations = any work/school tenant), not the trust list.
        // Acceptance is SiteSettings on /Admin/Authentication. A GUID here limits OIDC to that tenant.
        .WithEnvironment("Authentication__Entra__TenantId", builder.AddParameter(EntraTenantIdParameterName, "organizations", publishValueAsDefault: true))
        .WithEnvironment("Authentication__Entra__ClientId", builder.AddParameter(EntraClientIdParameterName, "", publishValueAsDefault: true))
        .WithEnvironment("Authentication__Entra__ClientSecret", builder.AddParameter(EntraClientSecretParameterName, entraClientSecret, secret: true))
        .WithEnvironment("Authentication__Entra__PublicOrigin", builder.AddParameter(EntraPublicOriginParameterName, "", publishValueAsDefault: true));
    }

    // Task 145-009 / dogfood: mock auth is OPT-IN only. Do not force Authentication:UseMock=true
    // for every Development AppHost run — that overrode appsettings (false) and turned on SPA/BFF
    // mock auth for local passkey dogfood. Forward the flag only when AppHost configuration
    // explicitly sets Authentication:UseMock (e.g. aspire-tests: --Authentication:UseMock=true).
    // Production never needs this injection; env vars still cannot enable mock outside
    // Development/Testing (handler/registration fail-closed gates). Run mode only (task 070-003):
    // published output must never carry the flag, whatever configuration the publish ran with.
    string? useMock = builder.Configuration["Authentication:UseMock"];
    if (builder.ExecutionContext.IsRunMode
      && (string.Equals(useMock, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(useMock, "1", StringComparison.OrdinalIgnoreCase)))
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
    IResourceBuilder<IResourceWithConnectionString> postgresDb;
    IResourceBuilder<IResourceWithConnectionString>? neonDirect = null;
    bool neonPostgres = string.Equals(publishTarget, ContainerAppsNeonPublishTarget, StringComparison.OrdinalIgnoreCase);
    if (neonPostgres)
    {
      // Task 317: Neon is not provisioned by this model. The pooled string is the app's connection.
      // The direct string is a second resource so migrations can reference it under the postgres-db
      // name without pointing web-server at the pooler. The pooled WithReference is applied after
      // AddEFMigrations (see the Design region). No WaitFor: a connection string has no health
      // endpoint, and publish mode does not run wait edges.
      // This is the only declaration of the two Neon parameters: web on, postgres on, Publish:Target=aca-neon.
      // Neither has a default. Publish still emits the pooled name as @secure(). Aspire also tries to copy
      // every parameter value into deployment state and logs MissingParameterValueException when the
      // operator has not set one. That warning does not fail publish. A default would ship the secret.
      IResourceBuilder<ParameterResource> pooled = builder.AddParameter(NeonPooledConnectionStringParameterName, secret: true);
      IResourceBuilder<ParameterResource> direct = builder.AddParameter(NeonDirectConnectionStringParameterName, secret: true);
      postgresDb = builder.AddConnectionString(PostgresDatabaseResourceName, ReferenceExpression.Create($"{pooled}"));
      neonDirect = builder.AddConnectionString(NeonDirectConnectionResourceName, ReferenceExpression.Create($"{direct}"));
    }
    else if (containerApps is not null)
    {
      // Task 070-007: Azure Database for PostgreSQL Flexible Server, not a postgres container on Azure
      // Files (no volume to lose, Azure-managed backups/patching). Password authentication with the
      // generated postgres-username / postgres-password parameters (the password @secure()). The
      // connection string lands in a Key Vault Aspire provisions beside the server, and web-server
      // reads it through a Key Vault-backed container-app secret; WithReference ALSO gives web-server
      // the password as container-app secrets postgres-db-password / postgres-db-uri (see the Design
      // region). Firewall: AllowAllAzureIps (any Azure IP, any tenant). Publish-only, like every target
      // switch: run mode never reaches this branch and keeps the container below.
      postgresDb = builder.AddAzurePostgresFlexibleServer(PostgresResourceName)
        .WithPasswordAuthentication()
        .AddDatabase(PostgresDatabaseResourceName);
    }
    else
    {
      bool usePostgresDataVolume = !string.Equals(
        builder.Configuration["Postgres:UseDataVolume"], "false", StringComparison.OrdinalIgnoreCase);

      IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres(PostgresResourceName);

      // Task 266 (Aspire 13.6): WithVolume(env:) replaces WithDataVolume() with the SAME volume
      // identity — the name WithDataVolume generates ({app}-{apphost-path hash}-postgres-data) and the
      // mount it picks for the default postgres 18.x image — so an existing dev volume is reused.
      // The env var carries the mount path, NOT PGDATA: postgres 18 images keep PGDATA in a
      // version subdirectory of the mount (see the constants.cs Design note).
      // Published (task 070-003) the volume is always on and gets a FIXED name: the generated name
      // hashes the AppHost path, so every checkout would emit a different compose.yaml. Compose
      // prefixes it with the project name, so it never collides with the dev-loop volume.
      if (builder.ExecutionContext.IsPublishMode)
      {
        postgres = postgres.WithVolume(
          PostgresPublishedDataVolumeName,
          PostgresDataVolumeTarget,
          env: PostgresDataVolumeEnvironmentVariable);
      }
      else if (usePostgresDataVolume)
      {
        postgres = postgres.WithVolume(
          VolumeNameGenerator.Generate(postgres, "data"),
          PostgresDataVolumeTarget,
          env: PostgresDataVolumeEnvironmentVariable);
      }

      // Task 262 (Aspire 13.6): dashboard "REPL" command opens an authenticated psql shell in the
      // dashboard terminal dock. Anyone who can run dashboard commands gets the server credentials,
      // so it is Development-only (a tunnelled/shared dashboard in any other environment never
      // exposes it). WithRepl itself is run-mode only, so publish output is unaffected.
      if (builder.Environment.IsDevelopment())
      {
        postgres = postgres.WithRepl();
      }

      // Run mode creates the database through the AppHost; a published Compose stack has no AppHost,
      // so the postgres image's own POSTGRES_DB creates it on first initdb.
      if (builder.ExecutionContext.IsPublishMode)
      {
        postgres = postgres.WithEnvironment("POSTGRES_DB", PostgresDatabaseResourceName);
      }

      // Task 070-004: in the Helm chart the published postgres-data volume binds (by name) to a
      // PersistentVolumeClaim postgres-data, which renders postgres as a single-replica StatefulSet
      // mounting that claim.
      if (kubernetes is not null)
      {
        postgres = postgres.WithPersistentVolume(
          kubernetes.AddPersistentVolume(PostgresPublishedDataVolumeName)
            .WithCapacity(builder.AddParameter(PostgresStorageCapacityParameterName, "10Gi", publishValueAsDefault: true)));
      }

      postgresDb = postgres.AddDatabase(PostgresDatabaseResourceName);
    }

    if (!neonPostgres)
    {
      webServer = webServer.WithReference(postgresDb).WaitFor(postgresDb);
    }

    // Task 147-007: first-class EF migrations resource (aspire.dev AddEFMigrations).
    // Migrations live in web-infrastructure (not an AppHost project resource — path form; typed
    // Projects.* is only generated for Aspire project resources).
    string webInfrastructureProject = Path.GetFullPath(
      Path.Combine(builder.AppHostDirectory, "../../../web/projects/web-infrastructure/web-infrastructure.csproj"));
    IResourceBuilder<IResourceWithConnectionString> migrationConnection = neonDirect ?? postgresDb;
    IResourceBuilder<global::Aspire.Hosting.EntityFrameworkCore.EFMigrationResource> webMigrations = webServer
      .AddEFMigrations(WebMigrationsResourceName, "TimeWarp.Architecture.Persistence.PostgresDbContext")
      .WithMigrationsProject(webInfrastructureProject)
      .WithMigrationOutputDirectory("../../platform/postgres/migrations")
      .WithMigrationNamespace("TimeWarp.Architecture.Persistence.Migrations")
      .WithReference(migrationConnection, PostgresDatabaseResourceName)
      .RunDatabaseUpdateOnStart()
      .PublishAsMigrationScript();

    if (neonDirect is null)
    {
      webMigrations = webMigrations.WaitFor(postgresDb);
    }
    else
    {
      // Wake-up budget for Neon Free's suspend. The default retry (4 × 2s) is unchanged on every other target.
      // After AddEFMigrations so the snapshot on ef-tool-web-migrations does not also carry the pooled string.
      webServer = webServer
        .WithReference(postgresDb)
        .WithEnvironment("Postgres__RetryCount", AcaNeonCompute.RetryCount)
        .WithEnvironment("Postgres__RetryDelaySeconds", AcaNeonCompute.RetryDelaySeconds);
    }

    // Task 070-004: no bundle in the Helm chart. Publish output IS the chart directory, and Helm
    // refuses to load a chart holding the ~100 MB self-contained bundle (5 MiB per-file limit), so
    // helm lint and `aspire deploy` would both fail. Kubernetes migrates from the SQL script.
    if (kubernetes is null)
    {
      webMigrations.PublishAsMigrationBundle();
    }
    // Task 155 / 270: no wait edge between web-server and web-migrations. Without a wait edge, a
    // dashboard restart/rebuild of web-server can never deadlock on the migration resource's
    // terminal Finished snapshot. WaitForCompletion still breaks DCP endpoint wiring on Aspire
    // 13.6 (see the Design region). web-server's boot seed waits until no migration is pending
    // (EfPendingMigrationsProbe, up to 180s — EF Core 11's verbose tool startup streams the
    // MSBuild item graph for about 90s before it migrates), so a first run or a new migration
    // logs no Error.
    // Re-run on demand via the ef-database-update dashboard command on the web-migrations resource.
#endif
    // Self-reference for the web server
    webServer.WithReference(webServer);
#endif
#if yarp
    // YARP Reverse Proxy
    // YARP is included in the template
    // Host-port pins are run-mode only: published, the ingress-port parameter owns the host port and
    // a pinned endpoint port would emit an invalid host:host:container mapping (task 070-003).
    bool isRunMode = builder.ExecutionContext.IsRunMode;
    int? ingressHttpsPort = isRunMode && int.TryParse(builder.Configuration["Ingress:Port"], out int httpsPort) ? httpsPort : null;
    int? ingressHttpPort = isRunMode && int.TryParse(builder.Configuration["Ingress:HttpPort"], out int httpPort) ? httpPort : null;

    string? ingressPublicUrl = builder.Configuration["Ingress:PublicUrl"];

    if (isRunMode)
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
      yarpProject = yarpProject.WithEndpoint("https", endpoint => endpoint.Port = ingressHttpsPort);

      if (ingressHttpPort is not null)
      {
        // Bind the pinned http port in-process. A DCP hop in front of it can replace
        // X-Forwarded-Proto with the scheme that hop saw (plain http) and drop Caddy's https
        // before UseTrustedProxyForwardedHeaders runs.
        yarpProject = yarpProject.WithEndpoint
        (
          "http",
          endpoint =>
          {
            endpoint.Port = ingressHttpPort.Value;
            endpoint.IsProxied = false;
          }
        );
      }
      else
      {
        yarpProject = yarpProject.WithEndpoint("http", endpoint => endpoint.Port = null);
      }

      if (!string.IsNullOrWhiteSpace(ingressPublicUrl))
      {
        // Display text IS the URL: a friendly label renders INSTEAD of the address in the
        // dashboard's URL column, hiding the hostname the link exists to surface.
        yarpProject = yarpProject.WithUrl(ingressPublicUrl, ingressPublicUrl);
      }

#if api
      yarpProject = yarpProject.WithReference(apiServer);
#endif
#if grpc
      yarpProject = yarpProject.WithReference(grpcServer);
#endif
#if web
      // Routes live in the yarp project's Development configuration (the same Set transform and
      // http://_http.web-server cluster the publish path builds below). WithReference injects
      // services__web-server__* so that address resolves. The http health check probes the
      // WebAliveRoute (IngressPaths.WebServerAlivePath -> web-server /alive), not the SPA shell;
      // with web excluded that route does not exist, so neither does the check.
      yarpProject = yarpProject.WithReference(webServer);
      yarpProject = yarpProject.WithHttpHealthCheck(global::TimeWarp.Foundation.Configuration.IngressPaths.WebServerAlivePath, endpointName: "http");

      // Task 335-002: the browser loads the SPA from the ingress, so the WASM exporter's span posts
      // carry the ingress origin. WithTimeWarpStateDevTools allows only web-server's own endpoints;
      // add the ingress https and http origins (and the public display URL when set) to the
      // inspector's CORS list. stateDevTools is always set here (run mode, web on).
      if (stateDevTools is not null)
      {
        ReferenceExpression ingressOrigins = string.IsNullOrWhiteSpace(ingressPublicUrl)
          ? ReferenceExpression.Create($"{yarpProject.GetEndpoint("https")},{yarpProject.GetEndpoint("http")}")
          : ReferenceExpression.Create($"{yarpProject.GetEndpoint("https")},{yarpProject.GetEndpoint("http")},{ingressPublicUrl}");
        stateDevTools.WithEnvironment(StateDevToolsCorsOriginsEnvironmentVariable, ingressOrigins);
      }
#endif
    }
    else
    {
      // Create the YARP resource
      IResourceBuilder<YarpResource> yarp = builder.AddYarp(YarpResourceName);

      // Task 070-003: published, the ingress is the ONLY service with a host port, pinned to the
      // ingress-port .env parameter (Compose would otherwise pick a random host port).
      if (kubernetes is not null)
      {
        // Task 070-004 ingress decision (a): the cluster's ingress controller forwards everything to the
        // YARP ingress, which keeps doing the per-service routing below. The Ingress resource's default
        // backend is YARP's http endpoint (TLS terminates at the controller); the class is a parameter
        // because the controller belongs to the cluster, not to this chart.
        yarp = yarp.WithExternalHttpEndpoints();
        kubernetes.AddIngress(KubernetesIngressResourceName)
          .WithIngressClass(builder.AddParameter(IngressClassParameterName, "nginx", publishValueAsDefault: true))
          .WithDefaultBackend(yarp.GetEndpoint("http"));
      }
      else if (containerApps is not null)
      {
        // Task 070-007: the ingress is the ONLY external container app (ACA's managed HTTPS ingress
        // with a public FQDN); web-server, api-server and grpc-server keep internal-only ingress.
        yarp = yarp.WithExternalHttpEndpoints();
        if (string.Equals(publishTarget, ContainerAppsNeonPublishTarget, StringComparison.OrdinalIgnoreCase))
        {
          yarp = yarp.PublishAsAzureContainerApp((_, app) => AcaNeonCompute.Apply(app));
        }
      }
      else if (builder.ExecutionContext.IsPublishMode)
      {
        // Task 311: the value is the committed Parameters:ingress-port (see Design), read here only
        // because publishValueAsDefault needs it at declaration time to write it into the .env.
        string ingressPortValue = builder.Configuration[$"Parameters:{IngressPortParameterName}"]
          ?? throw new InvalidOperationException($"Parameters:{IngressPortParameterName} is not set; commit it in the AppHost appsettings.json.");
        IResourceBuilder<ParameterResource> ingressPort = builder.AddParameter(IngressPortParameterName, ingressPortValue, publishValueAsDefault: true);
        yarp = yarp
          .WithExternalHttpEndpoints()
          .PublishAsDockerComposeService((composeService, service) =>
            service.Ports = [.. service.Ports.Select(containerPort => $"{ingressPort.AsEnvironmentPlaceholder(composeService)}:{containerPort}")]);
      }

      if (ingressHttpsPort is not null)
      {
        yarp = yarp.WithEndpoint("https", endpoint => endpoint.Port = ingressHttpsPort.Value);
      }

      if (ingressHttpPort is not null)
      {
        yarp = yarp.WithEndpoint("http", endpoint => endpoint.Port = ingressHttpPort.Value);
      }

      if (!string.IsNullOrWhiteSpace(ingressPublicUrl))
      {
        // Display text IS the URL: a friendly label ("public") renders INSTEAD of the address in the
        // dashboard's URL column, hiding the very hostname the link exists to surface.
        yarp = yarp.WithUrl(ingressPublicUrl, ingressPublicUrl);
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
        webServer = webServer.WithEnvironment(
          "WebAuthnOptions__AllowedRpIds__0",
          builder.AddParameter(PasskeyRpHostParameterName, passkeyRpHost!.Trim(), publishValueAsDefault: true));
      }

      if (containerApps is not null)
      {
        // Public output on Aspire.Hosting.Azure.AppContainers 13.6.4. ingress.<domain> does not
        // reference the ingress endpoint (that would cycle web-server and ingress).
        global::Aspire.Hosting.Azure.BicepOutputReference defaultDomain = containerApps.GetOutput(
          "AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN");
        string allowedRpIdIndex = extraPasskeyHost ? "1" : "0";
        webServer = webServer.WithEnvironment(
          $"WebAuthnOptions__AllowedRpIds__{allowedRpIdIndex}",
          ReferenceExpression.Create($"{YarpResourceName}.{defaultDomain}"));
      }

      yarp = yarp.WithReference(webServer);

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
        yarpConfiguration.AddRoute(global::TimeWarp.Foundation.Configuration.IngressPaths.GrpcPathPrefix + "/{**catch-all}", grpcServer)
          .WithTransformPathRemovePrefix(global::TimeWarp.Foundation.Configuration.IngressPaths.GrpcPathPrefix);
#endif
#if web
        // Catch-all to Web.Server (SPA + everything not owned above): same public-host forwarding as
        // the literal /api routes so RP-ID selection sees the public host (task 070-008).
        ForwardPublicHost(yarpConfiguration.AddRoute(webServerHttp));
#endif
      });
    }
#endif

#if api
    if (string.Equals(publishTarget, ContainerAppsNeonPublishTarget, StringComparison.OrdinalIgnoreCase))
    {
      apiServer.PublishAsAzureContainerApp((_, app) => AcaNeonCompute.Apply(app));
    }

#endif
#if grpc
    if (string.Equals(publishTarget, ContainerAppsNeonPublishTarget, StringComparison.OrdinalIgnoreCase))
    {
      grpcServer.PublishAsAzureContainerApp((_, app) => AcaNeonCompute.Apply(app));
    }

#endif
#if web
    if (string.Equals(publishTarget, ContainerAppsNeonPublishTarget, StringComparison.OrdinalIgnoreCase))
    {
      webServer.PublishAsAzureContainerApp((_, app) => AcaNeonCompute.Apply(app));
    }

#endif
    builder.Build().Run();
  }

  // Task 417. See the Design region. Outside every template flag: every generated app can link it.
  private static void AddSigNozDashboardLink(IDistributedApplicationBuilder builder)
  {
    const string configurationKey = "Observability:SigNoz:Url";
    const string resourceName = "signoz";
    const string displayText = "SigNoz";

    string? configuredUrl = builder.Configuration[configurationKey];
    if (string.IsNullOrWhiteSpace(configuredUrl))
    {
      return;
    }

    string candidate = configuredUrl.Trim();
    if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? signozUri)
      || signozUri.Scheme != Uri.UriSchemeHttps
      || string.IsNullOrEmpty(signozUri.Host))
    {
      throw new InvalidOperationException(
        $"{configurationKey} '{candidate}' must be an absolute https URL.");
    }

    if (!builder.ExecutionContext.IsRunMode)
    {
      return;
    }

    // AddExternalService already records the URL. A second WithUrl would add another link.
    IResourceBuilder<ExternalServiceResource> signoz = builder.AddExternalService(resourceName, signozUri);
    ResourceUrlAnnotation dashboardLink = signoz.Resource.Annotations.OfType<ResourceUrlAnnotation>().Single();
    dashboardLink.DisplayText = displayText;
  }
}
