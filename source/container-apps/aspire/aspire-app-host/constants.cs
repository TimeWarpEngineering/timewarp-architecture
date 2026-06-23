namespace TimeWarp.Architecture.Aspire;

internal class Constants
{
  public const string CosmosDbResourceName = "cosmos";
  public const string CosmosDbConnectionStringName = CosmosDbResourceName;
  public const string CosmosDbDatabaseName = "CosmosDbContext";
  // These MUST match ServiceNames.{Api,Web,Grpc}ServiceName (foundation-contracts) — the apps'
  // server-side ServiceUriHelper resolves BaseAddress from the injected services__{name}__https__0
  // env var, which Aspire keys by these resource names. (Also matches the Docker/K8s YARP config.)
  public const string ApiServerProjectResourceName = "api-server";
  public const string WebServerProjectResourceName = "web-server";
  public const string GrpcServerProjectResourceName = "grpc-server";
  public const string YarpProjectResourceName = "yarp";
  public const string YarpResourceName = "ingress";
}
