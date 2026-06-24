#nullable enable
namespace TimeWarp.Architecture.HostedServices;

public class CosmosDbContextStartupHostedService : IHostedService
{
  private static readonly Action<ILogger, Exception?> LogStarted =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(1, nameof(LogStarted)),
      $"{nameof(CosmosDbContextStartupHostedService)} has started."
    );

  private static readonly Action<ILogger, Exception?> LogStopped =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(2, nameof(LogStopped)),
      $"{nameof(CosmosDbContextStartupHostedService)} has stopped."
    );

  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public CosmosDbContextStartupHostedService
  (
    IServiceProvider serviceProvider,
    ILogger<CosmosDbContextStartupHostedService> logger
  )
  {
    ServiceProvider = serviceProvider;
    Logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    LogStarted(Logger, null);
    using IServiceScope scope = ServiceProvider.CreateScope();

    CosmosDbContext cosmosDbContext = scope.ServiceProvider.GetRequiredService<CosmosDbContext>();
    await cosmosDbContext.Database.EnsureCreatedAsync(cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    LogStopped(Logger, null);
    return Task.CompletedTask;
  }
}
