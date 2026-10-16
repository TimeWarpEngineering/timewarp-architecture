#region Purpose
// Paths every ContainerApp's MapDefaultEndpoints serves its health endpoints on, shared with the
// ingress routes and probes that target them.
#endregion

#region Design
// MapDefaultEndpoints (aspire-service-defaults) maps both paths; the ingress health route rewrites
// IngressPaths.WebServerAlivePath to Alive on web-server (task 377). Exposure per environment is
// recorded in MapDefaultEndpoints' Design region: Alive in every environment, Health only in
// Development.
#endregion

namespace TimeWarp.Foundation.Configuration;

/// <summary>
/// Health endpoint paths mapped by the service defaults.
/// </summary>
public static class HealthEndpointPaths
{
  /// <summary>
  /// Liveness: only checks tagged "live". Mapped in every environment.
  /// </summary>
  public const string Alive = "/alive";
  /// <summary>
  /// Readiness: every registered health check. Mapped in Development only.
  /// </summary>
  public const string Health = "/health";
}
