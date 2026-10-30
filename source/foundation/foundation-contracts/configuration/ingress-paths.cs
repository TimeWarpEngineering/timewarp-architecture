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
// WebServerHealthPath and TipDiscoveryAliasPath (task 439) are web-server routes under /api that are
// not ApiRoute prefixes, so WebServerApiRoutePrefixes.All does not carry them. Both ingress builders
// add each as one exact route to web-server, so it outranks the Api.Server /api/{**catch-all}. The
// alias is one route: a route template ignores a trailing slash, so /api/ matches it, and a second
// "/api/" route ties with it (AmbiguousMatchException). The OpenAPI document advertises both paths
// on the ingress origin; StandaloneYarpIngress_ requests them through the gateway.
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

  /// <summary>
  /// Public readiness path the ingress routes to web-server unchanged (every registered health check).
  /// </summary>
  public const string WebServerHealthPath = "/api/health";

  /// <summary>
  /// Bare API root the ingress routes to web-server, which rewrites it to the tip action.
  /// Also matches <c>/api/</c>.
  /// </summary>
  public const string TipDiscoveryAliasPath = "/api";
}
