namespace TimeWarp.Architecture.Services;

public class SuperheroGrpcServiceProvider
{
  private readonly ServiceUriProvider ServiceUriProvider;
  private readonly ILogger<SuperheroGrpcServiceProvider> Logger;
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

    if (ServiceUriProvider.ServiceUris.TryGetValue(Constants.GrpcServiceName, out Uri? grpcUri))
    {
      var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());

      var grpcChannel =
        GrpcChannel.ForAddress
        (
          grpcUri,
          new GrpcChannelOptions
          {
            HttpHandler = httpHandler,
            // Additional options can be set here
          }
        );

      Logger.LogInformation("gRPC service initialized successfully.");
      CachedSuperheroService = grpcChannel.CreateGrpcService<ISuperheroService>();
      return CachedSuperheroService;
    }

    Logger.LogError("gRPC URI not found in service discovery.");
    throw new InvalidOperationException("gRPC URI not found in service discovery.");
  }
}
