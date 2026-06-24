#nullable enable
// TOOD: This is copilot generated code, it needs to be reviewed and cleaned up
namespace TimeWarp.Architecture.Configuration;

public class PostgresDbEnvironmentCheck
{
  private static readonly Action<ILogger, Exception?> LogStart =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(1, nameof(LogStart)),
      $"Start {nameof(PostgresDbEnvironmentCheck)} "
    );

  private static readonly Action<ILogger, Exception?> LogCompleted =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(2, nameof(LogCompleted)),
      $"Completed {nameof(PostgresDbEnvironmentCheck)} "
    );

  private readonly PostgresDbOptions PostgresDbOptions;
  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public PostgresDbEnvironmentCheck
  (
      IOptions<PostgresDbOptions> postgresDbOptionsAccessor,
      IServiceProvider serviceProvider,
      ILogger<PostgresDbEnvironmentCheck> logger
  )
  {
    PostgresDbOptions = postgresDbOptionsAccessor.Value;
    ServiceProvider = serviceProvider;
    Logger = logger;
  }

  public static string Description => "Connecting to PostgreSQL";

  public async Task<bool> CheckAsync()
  {
    LogStart(Logger, null);

    using IServiceScope scope = ServiceProvider.CreateScope();

    PostgresDbContext postgresDbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

    try
    {
      await postgresDbContext.Database.CanConnectAsync().ConfigureAwait(true);
    }
    catch (HttpRequestException)
    {
      return false;
    }

    LogCompleted(Logger, null);
    return true;
  }
}
