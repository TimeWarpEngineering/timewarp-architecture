#region Purpose
// Aspire AppHost resource names: project resources aliased to ServiceNames for service discovery,
// plus the Postgres container/database resource names (the database name doubles as its ConnectionStrings key).
#endregion

namespace TimeWarp.Architecture.Aspire;

using TimeWarp.Foundation.Configuration;

internal class Constants
{
  // Aliases of ServiceNames.* — the apps' server-side ServiceUriHelper resolves BaseAddress from
  // the injected services__{name}__https__0 env var, which Aspire keys by these resource names.
  // Const-to-const aliasing makes drift impossible; TWA0007 additionally guards any
  // hand-written AddProject name. (Also matches the Docker/K8s YARP config.)
  public const string ApiServerProjectResourceName = ServiceNames.ApiServiceName;
  public const string WebServerProjectResourceName = ServiceNames.WebServiceName;
  public const string GrpcServerProjectResourceName = ServiceNames.GrpcServiceName;
  public const string YarpProjectResourceName = ServiceNames.YarpServiceName;
  public const string YarpResourceName = "ingress";

  // Postgres resource names are intentionally NOT ServiceNames.* values: TWA0007 and service
  // discovery govern AddProject resources only (server-side BaseAddress resolution), and Postgres
  // is a container resource, not a project. The database resource name doubles as the
  // ConnectionStrings key Aspire injects (ConnectionStrings__postgres-db), which is why
  // PostgresDbModule reads the connection string by this same constant. This file is compile-linked
  // into web-server (see web-server.csproj), so AppHost and PostgresDbModule reference one constant
  // and cannot drift.
  public const string PostgresResourceName = "postgres";
  public const string PostgresDatabaseResourceName = "postgres-db";

  // Task 266: data volume mount for the default postgres image (18.x). Aspire's WithDataVolume
  // picks /var/lib/postgresql for 18+ (docker-library/postgres#1259 moved the volume up a level so
  // pg_upgrade --link works); the image's own PGDATA is /var/lib/postgresql/18/docker, INSIDE this
  // mount — so the env var below names the mount, never PGDATA. Pinning an image <= 17 would need
  // /var/lib/postgresql/data here; aspire-tests compares this against WithDataVolume's choice.
  public const string PostgresDataVolumeTarget = "/var/lib/postgresql";
  public const string PostgresDataVolumeEnvironmentVariable = "POSTGRES_DATA_VOLUME";

  // Task 147-007: Aspire AddEFMigrations resource name (not an AddProject — TWA0007 N/A; keep const
  // alongside Postgres names so AppHost wiring cannot drift from docs/scripts).
  public const string WebMigrationsResourceName = "web-migrations";

  // Task 070-003: Docker Compose publish environment (also the `aspire do prepare-<name>` step name),
  // its fixed data volume name, and the publish-only parameters that land in the generated .env.
  public const string ComposeEnvironmentResourceName = "compose";
  public const string PostgresPublishedDataVolumeName = "postgres-data";
  public const string IngressPortParameterName = "ingress-port";
  public const string EntraEnabledParameterName = "entra-enabled";
  public const string EntraTenantIdParameterName = "entra-tenant-id";
  public const string EntraClientIdParameterName = "entra-client-id";
  public const string EntraClientSecretParameterName = "entra-client-secret";
  public const string EntraPublicOriginParameterName = "entra-public-origin";

  // Task 070-004: publish target switch. Aspire assigns each compute resource to exactly ONE compute
  // environment, so Compose and Kubernetes cannot both publish one model; `aspire publish -- --Publish:Target=kubernetes`
  // selects the Helm chart (default: compose).
  public const string PublishTargetConfigurationKey = "Publish:Target";
  public const string ComposePublishTarget = "compose";
  public const string KubernetesPublishTarget = "kubernetes";
  public const string ContainerAppsPublishTarget = "aca";

  // Task 070-004: Kubernetes publish environment (Helm chart), its cluster Ingress, container registry,
  // and the publish-only parameters for Helm placement, registry and storage.
  public const string KubernetesEnvironmentResourceName = "k8s";
  public const string KubernetesIngressResourceName = "cluster-ingress";
  public const string ContainerRegistryResourceName = "registry";
  public const string KubernetesNamespaceParameterName = "k8s-namespace";
  public const string HelmReleaseNameParameterName = "helm-release-name";
  public const string HelmChartVersionParameterName = "helm-chart-version";
  public const string IngressClassParameterName = "ingress-class";
  public const string RegistryEndpointParameterName = "registry-endpoint";
  public const string RegistryRepositoryParameterName = "registry-repository";
  public const string PostgresStorageCapacityParameterName = "postgres-storage-capacity";

  // Task 070-007: Azure Container Apps publish environment (Bicep). Its registry, Log Analytics workspace
  // and the Flexible Server's Key Vault are named by Aspire from this and the postgres resource name.
  public const string ContainerAppsEnvironmentResourceName = "aca-env";
}
