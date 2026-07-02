#region Purpose
// Aspire AppHost resource names, pinned to the ServiceNames the apps use for service discovery.
#endregion

namespace TimeWarp.Architecture.Aspire;

using TimeWarp.Foundation.Configuration;

internal class Constants
{
  // Aliases of ServiceNames.* — the apps' server-side ServiceUriHelper resolves BaseAddress from
  // the injected services__{name}__https__0 env var, which Aspire keys by these resource names.
  // Const-to-const aliasing makes drift impossible; TWPA0007 additionally guards any
  // hand-written AddProject name. (Also matches the Docker/K8s YARP config.)
  public const string ApiServerProjectResourceName = ServiceNames.ApiServiceName;
  public const string WebServerProjectResourceName = ServiceNames.WebServiceName;
  public const string GrpcServerProjectResourceName = ServiceNames.GrpcServiceName;
  public const string YarpProjectResourceName = ServiceNames.YarpServiceName;
  public const string YarpResourceName = "ingress";
}