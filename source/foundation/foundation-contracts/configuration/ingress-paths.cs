#region Purpose
// Path prefixes the public ingress routes to a backend service, shared by the AppHost's YARP routes
// and the browser clients that call through the ingress origin.
#endregion

#region Design
// The browser never learns internal service URIs: every browser-facing call goes to the ingress
// origin, and the ingress routes by path prefix (task 312). The AppHost route and the WASM
// client's BaseAddress both read this constant so they cannot drift. The yarp project's
// appsettings.Development.json (in-proc YARP) still spells the literal — JSON cannot reference it.
#endregion

namespace TimeWarp.Foundation.Configuration;

/// <summary>
/// Path prefixes the ingress strips before forwarding to a backend service.
/// </summary>
public static class IngressPaths
{
  /// <summary>
  /// Prefix the ingress routes to the gRPC server (removed before forwarding).
  /// </summary>
  public const string GrpcPathPrefix = "/grpc";
}
