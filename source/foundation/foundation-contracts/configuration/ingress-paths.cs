#region Purpose
// Path prefixes the public ingress routes to a backend service, shared by the AppHost's YARP routes
// and the browser clients that call through the ingress origin.
#endregion

#region Design
// The browser never learns internal service URIs: every browser-facing call goes to the ingress
// origin, and the ingress routes by path prefix (task 312). The AppHost route and the WASM
// client's BaseAddress both read this constant. The yarp project's appsettings.Development.json
// (the run-mode ingress) spells the literal in its GrpcRoute path and PathRemovePrefix, because JSON
// cannot reference a constant; the yarp integration test GrpcRouteIngressPath_ fails
// when that literal and this constant disagree.
// WebServerAlivePath (task 377) is the ingress health probe: a literal route that outranks the web
// catch-all and rewrites to web-server's liveness endpoint, so a probe proves ingress-to-web-server
// routing without prerendering the SPA shell. The Development WebAliveRoute spells it as a literal;
// the yarp integration test WebAliveRouteIngressPath_ is the agreement check.
#endregion

namespace TimeWarp.Foundation.Configuration;

/// <summary>
/// Paths the ingress routes to a backend service.
/// </summary>
public static class IngressPaths
{
  /// <summary>
  /// Prefix the ingress routes to the gRPC server (removed before forwarding).
  /// </summary>
  public const string GrpcPathPrefix = "/grpc";

  /// <summary>
  /// Ingress path the ingress health check probes; routed to web-server's
  /// <see cref="HealthEndpointPaths.Alive"/>.
  /// </summary>
  public const string WebServerAlivePath = "/web-server" + HealthEndpointPaths.Alive;
}
