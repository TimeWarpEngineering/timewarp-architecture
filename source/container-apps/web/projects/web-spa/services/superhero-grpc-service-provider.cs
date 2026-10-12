#region Purpose
// Lazily builds and caches the gRPC-Web channel/client for ISuperheroService from the host's
// ServiceNames.GrpcServiceName named HttpClient.
#endregion

#region Design
// Browsers cannot speak native gRPC (no HTTP/2 trailer access), so the channel is wrapped in
// GrpcWebHandler with GrpcWebMode.GrpcWeb. The same provider runs in both hosts; each host owns the
// named client's BaseAddress (task 312):
//   - WASM (Web.Spa.Program.Main): the ingress origin plus IngressPaths.GrpcPathPrefix. The browser
//     never learns internal service URIs; the ingress strips the prefix and forwards to grpc-server.
//   - Web.Server (prerender + InteractiveServer): https+http://grpc-server through Aspire service
//     discovery, which AddServiceDefaults attaches to every named client's handler pipeline.
// GrpcChannel drops an address path (it logs AddressPathUnused) and accepts only http/https
// addresses, so neither BaseAddress can be the channel address. GrpcBaseAddressHandler rebases each
// call onto the named client's BaseAddress instead, and sits OUTSIDE the factory's pipeline
// (CreateHandler) so service discovery sees https+http://grpc-server/… and resolves it. The channel
// address only seeds the request path; its scheme is http on purpose: GrpcWebHandler relaxes HTTP/2
// to "or lower" only for an https request URI, and grpc-server's endpoints are HTTP/2-only, so the
// server-side hop keeps HTTP/2 exact — ALPN on https (run mode), h2c on the published http fallback.
// The browser ignores the request version. A host that never registered the client fails here, loudly,
// on first use — never a silent null BaseAddress.
// Channel and handlers are cached for the provider's lifetime — creating a channel per call is
// expensive. Disposing the handler chain stops at the factory handler, whose lifetime the factory
// owns (its Dispose is a no-op).
#endregion

namespace TimeWarp.Architecture.Services;

public sealed class SuperheroGrpcServiceProvider : IDisposable
{
  private readonly IHttpClientFactory HttpClientFactory;
  private readonly IHttpMessageHandlerFactory HttpMessageHandlerFactory;
  private GrpcChannel? CachedGrpcChannel;
  private GrpcWebHandler? CachedGrpcWebHandler;
  private ISuperheroService? CachedSuperheroService;

  public SuperheroGrpcServiceProvider(IHttpClientFactory httpClientFactory, IHttpMessageHandlerFactory httpMessageHandlerFactory)
  {
    HttpClientFactory = httpClientFactory;
    HttpMessageHandlerFactory = httpMessageHandlerFactory;
  }

  public ISuperheroService GetGrpcService()
  {
    if (CachedSuperheroService is not null) return CachedSuperheroService;

    Uri baseAddress;
    using (HttpClient grpcHttpClient = HttpClientFactory.CreateClient(ServiceNames.GrpcServiceName))
    {
      baseAddress = grpcHttpClient.BaseAddress
        ?? throw new InvalidOperationException($"The '{ServiceNames.GrpcServiceName}' HttpClient has no BaseAddress; the host must register it.");
    }

    GrpcBaseAddressHandler? grpcBaseAddressHandler = null;
    try
    {
      grpcBaseAddressHandler = new GrpcBaseAddressHandler(baseAddress, HttpMessageHandlerFactory.CreateHandler(ServiceNames.GrpcServiceName));
      CachedGrpcWebHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, grpcBaseAddressHandler);
      grpcBaseAddressHandler = null;
    }
    finally
    {
      grpcBaseAddressHandler?.Dispose();
    }

    CachedGrpcChannel =
      GrpcChannel.ForAddress
      (
        new UriBuilder(Uri.UriSchemeHttp, baseAddress.Host).Uri,
        new GrpcChannelOptions { HttpHandler = CachedGrpcWebHandler }
      );

    CachedSuperheroService = CachedGrpcChannel.CreateGrpcService<ISuperheroService>();
    return CachedSuperheroService;
  }

  public void Dispose()
  {
    // The channel does not dispose a supplied HttpHandler (DisposeHttpClient defaults to false).
    CachedGrpcChannel?.Dispose();
    CachedGrpcWebHandler?.Dispose();
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Rebases each gRPC call (<c>/{service}/{method}</c>) onto the named client's BaseAddress.
  /// </summary>
  private sealed class GrpcBaseAddressHandler(Uri baseAddress, HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
  {
    private readonly Uri BaseAddress = baseAddress.AbsoluteUri.EndsWith('/', StringComparison.Ordinal) ? baseAddress : new Uri(baseAddress.AbsoluteUri + "/");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      if (request.RequestUri is not null)
      {
        request.RequestUri = new Uri(BaseAddress, request.RequestUri.PathAndQuery.TrimStart('/'));
      }

      return base.SendAsync(request, cancellationToken);
    }
  }
}
