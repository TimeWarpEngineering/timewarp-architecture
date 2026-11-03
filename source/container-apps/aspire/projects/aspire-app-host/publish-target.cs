#region Purpose
// Reads Publish:Target (publish mode only) and declares the one compute environment the AppHost publishes to.
#endregion

#region Design
// Docker Compose publish target (task 070-003): AddDockerComposeEnvironment makes `aspire publish` emit
// docker-compose.yaml + .env (unfilled parameters) for standalone hardware; `aspire do prepare-compose`
// or `aspire deploy` fill the .env and build images through whichever container runtime Aspire detects
// (ASPIRE_CONTAINER_RUNTIME overrides). A compute environment only shapes publish/deploy, so run mode
// is unchanged; every publish-only branch in this AppHost is IsPublishMode/IsRunMode-gated, never flag-gated, so
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
// reach. Re-running the script is a no-op; web-server tolerates the not-yet-migrated window (PostgresResources Design).
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

/// <summary>The selected publish target and the compute environment declared for it.</summary>
/// <param name="Name">The Publish:Target value; always compose in run mode.</param>
/// <param name="Kubernetes">The Helm chart environment when the target is kubernetes.</param>
/// <param name="ContainerApps">The Azure Container Apps environment when the target is aca or aca-neon.</param>
internal sealed record PublishTarget
(
  string Name,
  IResourceBuilder<KubernetesEnvironmentResource>? Kubernetes,
  IResourceBuilder<AzureContainerAppEnvironmentResource>? ContainerApps
)
{
  /// <summary>True for Publish:Target=aca-neon (task 317).</summary>
  internal bool IsContainerAppsNeon =>
    string.Equals(Name, ContainerAppsNeonPublishTarget, StringComparison.OrdinalIgnoreCase);

  /// <summary>Reads the target and declares its compute environment. An unknown target throws.</summary>
  internal static PublishTarget Declare(IDistributedApplicationBuilder builder)
  {
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
      // Task 317: the same environment as aca. PostgresResources wires Postgres as Neon connection strings, not
      // a Flexible Server, so this branch only declares the environment. The container-app scale
      // (min replicas 0, small CPU and memory) is applied to each compute resource by AcaNeonCompute.ApplyTo.
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

    return new PublishTarget(publishTarget, kubernetes, containerApps);
  }
}
