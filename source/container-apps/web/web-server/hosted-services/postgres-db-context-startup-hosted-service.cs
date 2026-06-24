#nullable enable
//TODO - the is copilot generated code, it needs to be reviewed and cleaned up
namespace TimeWarp.Architecture.HostedServices;

public sealed partial class PostgresDbContextStartupHostedService : IHostedService
{
  private static readonly Action<ILogger, Exception?> LogStarted =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(1, nameof(LogStarted)),
      $"{nameof(PostgresDbContextStartupHostedService)} has started."
    );

  private static readonly Action<ILogger, Exception?> LogStopped =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(2, nameof(LogStopped)),
      $"{nameof(PostgresDbContextStartupHostedService)} has stopped."
    );

  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public PostgresDbContextStartupHostedService
  (
       IServiceProvider serviceProvider,
          ILogger<PostgresDbContextStartupHostedService> logger
     )
  {
    ServiceProvider = serviceProvider;
    Logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    LogStarted(Logger, null);
    using IServiceScope scope = ServiceProvider.CreateScope();

    PostgresDbContext postgresDbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
    await postgresDbContext.Database.EnsureCreatedAsync(cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    LogStopped(Logger, null);
    return Task.CompletedTask;
  }
}
