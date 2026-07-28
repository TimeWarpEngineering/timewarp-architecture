#region Purpose
// Lazily builds and caches the gRPC-Web channel/client for ISuperheroService in the WASM client.
#endregion

#region Design
// Browsers cannot speak native gRPC (no HTTP/2 trailer access), so the channel is wrapped in
// GrpcWebHandler with GrpcWebMode.GrpcWeb.
// Construction is deferred until first use because the target URI comes from ServiceUriProvider,
// which itself requires an async fetch; DI constructors cannot await that.
// Channel, handler, and typed client are cached for the provider's lifetime — creating a channel
// per call is expensive. The null-swap/try-finally dance ensures handlers are disposed if channel
// creation throws, without double-disposing on success.
#endregion

namespace TimeWarp.Architecture.Services;

public sealed class SuperheroGrpcServiceProvider : IDisposable
{
  private readonly ServiceUriProvider ServiceUriProvider;
  private readonly ILogger<SuperheroGrpcServiceProvider> Logger;
  private GrpcChannel? CachedGrpcChannel;
  private GrpcWebHandler? CachedHttpHandler;
  private ISuperheroService? CachedSuperheroService;

  public SuperheroGrpcServiceProvider(ServiceUriProvider serviceUriProvider, ILogger<SuperheroGrpcServiceProvider> logger)
  {
    ServiceUriProvider = serviceUriProvider;
    Logger = logger;
  }

  public async Task<ISuperheroService> GetGrpcServiceAsync(CancellationToken cancellationToken)
  {
    if (CachedSuperheroService is not null) return CachedSuperheroService;

    Logger.LogInformation("Initializing gRPC service...");

    // Ensure the ServiceUriProvider is initialized
    await ServiceUriProvider.InitializeAsync(cancellationToken);

    if (ServiceUriProvider.ServiceUris.TryGetValue(ServiceNames.GrpcServiceName, out Uri? grpcUri))
    {
      HttpClientHandler? httpClientHandler = null;
      GrpcWebHandler? httpHandler = null;

      try
      {
        httpClientHandler = new HttpClientHandler();
        httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, httpClientHandler);
        httpClientHandler = null;

        CachedGrpcChannel =
          GrpcChannel.ForAddress
          (
            grpcUri,
            new GrpcChannelOptions
            {
              HttpHandler = httpHandler,
              // Additional options can be set here
            }
          );

        CachedHttpHandler = httpHandler;
        httpHandler = null;
      }
      finally
      {
        httpHandler?.Dispose();
        httpClientHandler?.Dispose();
      }

      Logger.LogInformation("gRPC service initialized successfully.");
      CachedSuperheroService = CachedGrpcChannel.CreateGrpcService<ISuperheroService>();
      return CachedSuperheroService;
    }

    Logger.LogError("gRPC URI not found in service discovery.");
    throw new InvalidOperationException("gRPC URI not found in service discovery.");
  }

  public void Dispose()
  {
    CachedGrpcChannel?.Dispose();
    CachedHttpHandler?.Dispose();
    GC.SuppressFinalize(this);
  }
}
