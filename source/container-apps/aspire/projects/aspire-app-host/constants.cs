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

  // Task 147-007: Aspire AddEFMigrations resource name (not an AddProject — TWA0007 N/A; keep const
  // alongside Postgres names so AppHost wiring cannot drift from docs/scripts).
  public const string WebMigrationsResourceName = "web-migrations";
}
