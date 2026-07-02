#region Purpose
// Single source of truth for service-discovery names shared by the Aspire AppHost and every client that resolves a service BaseAddress.
#endregion

#region Design
// Aspire AppHost resource names must equal these constants: service discovery resolves
// addresses by name, and a mismatch yields a null BaseAddress during server-side rendering.
#endregion

namespace TimeWarp.Foundation.Configuration;
public static class ServiceNames
{
  public const string ApiServiceName = "api-server";
  public const string GrpcServiceName = "grpc-server";
  public const string WebServiceName = "web-server";
  public const string YarpServiceName = "yarp";
}
