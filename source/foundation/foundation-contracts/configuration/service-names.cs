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
  /// Service-discovery name reserved for a yarp project resource. The public ingress resource is
  /// <see cref="IngressServiceName"/>.
  /// </summary>
  public const string YarpServiceName = "yarp";

  /// <summary>
  /// Aspire resource name of the public ingress. Run mode hosts the yarp project under this name;
  /// publish mode hosts the AddYarp resource under the same name.
  /// </summary>
  public const string IngressServiceName = "ingress";
}
