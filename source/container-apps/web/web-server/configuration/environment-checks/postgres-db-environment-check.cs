#region Purpose
// Oakton environment check that fails fast at startup when PostgreSQL is unreachable.
#endregion

#region Design
// Registered as a singleton via PostgresDbModule and run by `RunOaktonCommands` before the app
// serves traffic — a bad connection string surfaces as a check failure, not a mid-request error.
// Creates its own scope because PostgresDbContext is scoped while this check is a singleton.
// Uses CanConnectAsync as the probe so the check needs no schema; LoggerMessage.Define keeps
// logging allocation-free per the repo's logging convention.
#endregion

#nullable enable
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

    bool canConnect;
    try
    {
      canConnect = await postgresDbContext.Database.CanConnectAsync().ConfigureAwait(true);
    }
    catch (Exception)
    {
      // Any failure reaching the database (network, auth, provider) fails the startup gate.
      return false;
    }

    LogCompleted(Logger, null);
    return canConnect;
  }
}
