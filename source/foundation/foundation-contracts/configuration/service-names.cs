#region Purpose
// Single source of truth for service-discovery names shared by the Aspire AppHost and every client that resolves a service BaseAddress.
#endregion

#region Design
// Aspire AppHost resource names must equal these constants: service discovery resolves
// addresses by name, and a mismatch yields a null BaseAddress during server-side rendering.
#endregion

namespace TimeWarp.Foundation.Configuration;

/// <summary>
/// Aspire resource names used for service discovery across AppHost and clients.
/// </summary>
public static class ServiceNames
{
  /// <summary>
  /// Aspire resource name for the API server.
  /// </summary>
  public const string ApiServiceName = "api-server";
  /// <summary>
  /// Aspire resource name for the gRPC server.
  /// </summary>
  public const string GrpcServiceName = "grpc-server";
  /// <summary>
  /// Aspire resource name for the web server.
  /// </summary>
  public const string WebServiceName = "web-server";
  /// <summary>
  /// Aspire resource name for the YARP ingress.
  /// </summary>
  public const string YarpServiceName = "yarp";
}
