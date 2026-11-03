#region Purpose
// Web.Server's server-side named HttpClients, resolved through Aspire service discovery.
#endregion

#region Design
// Task 312: server-side named HttpClients (web-server self-call, api-server, grpc-server) use
// https+http://{ServiceNames.*} through Aspire service discovery, which AddServiceDefaults
// registers for every client. https+http prefers https (run mode, dev cert) and falls back to http
// (Compose, Kubernetes and ACA expose http only). Never hand-read services__* env vars: the old
// https-only read gave a null BaseAddress on every published target. An unresolvable name fails
// the first call with the service name in the error. The browser never receives service URIs:
// WASM clients target the ingress origin, so there is no /service-discovery endpoint.
// Task 205-003: the web-server self-call attaches IdentitySessionCookieForwardingHandler so
// InteractiveServer/Auto API calls (Profile PUT) present the inbound identity-session cookie
// instead of challenging 401. Tasks 213 / 312: that handler also forwards the circuit's public
// origin on X-Forwarded-Host (port kept) and X-Forwarded-Proto (never HTTP Host — TLS on the https
// hop validates the discovered host), so passkey RP-ID selection and absolute mail links see the
// browser's origin on both the run-mode https://localhost hop and the published http://web-server
// hop. On published targets that hop is cleartext inside the deployment network, the same as
// ingress → web-server.
// Missing discovery config: with no Services:web-server:* entry, service discovery's pass-through
// resolver does not fail — it resolves https://web-server through DNS (search domains included),
// and the forwarding handler still sends the Cookie and mock-principal headers. https is tried
// first, so TLS certificate name validation for "web-server" is what keeps those credentials off a
// foreign endpoint (an internal CA issuing that name would defeat it). Configuring
// Services:web-server:http:0 with an external URL sends them in cleartext; that takes control of
// configuration. Aspire always injects the entry, so this only bites a host started outside Aspire.
#endregion

namespace TimeWarp.Architecture.Web.Server;
/// <summary>Registers the default and the named service-discovery HttpClients.</summary>
internal static class WebServerHttpClients
{
  /// <summary>Default client, Entra tenant resolver, and the web-server/api-server/grpc-server named clients.</summary>
  internal static void ConfigureServices(IServiceCollection serviceCollection)
  {
    serviceCollection.AddHttpClient();
    EntraTenantResolver.Register(serviceCollection);
    serviceCollection.AddTransient<IdentitySessionCookieForwardingHandler>();
    // Task 312: Aspire service discovery (AddServiceDefaults) resolves these; https+http prefers
    // https (run mode) and falls back to http (Compose, Kubernetes, ACA publish http only).
    serviceCollection.AddHttpClient(ServiceNames.WebServiceName, client => client.BaseAddress = ServiceDiscoveryAddress(ServiceNames.WebServiceName))
      .AddHttpMessageHandler<IdentitySessionCookieForwardingHandler>();
    serviceCollection.AddHttpClient(ServiceNames.ApiServiceName, client => client.BaseAddress = ServiceDiscoveryAddress(ServiceNames.ApiServiceName));
#if grpc
    // Server-side half of the SPA's gRPC client (prerender + InteractiveServer). The browser half
    // targets the ingress origin plus IngressPaths.GrpcPathPrefix — see Web.Spa.Program.
    serviceCollection.AddHttpClient(ServiceNames.GrpcServiceName, client => client.BaseAddress = ServiceDiscoveryAddress(ServiceNames.GrpcServiceName));
#endif
  }

  /// <summary>
  /// The Aspire service-discovery address for a sibling (or this) service:
  /// <c>https+http://{serviceName}/</c> — https when the target exposes it, else http.
  /// </summary>
  private static Uri ServiceDiscoveryAddress(string serviceName) => new($"https+http://{serviceName}/");
}
