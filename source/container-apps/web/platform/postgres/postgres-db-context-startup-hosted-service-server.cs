#region Purpose
// Materializes / evolves PostgreSQL schema at host startup so the template matches the EF model.
#endregion

#region Design
// Delegates to PostgresModelSchemaBootstrap: EnsureCreated for empty DBs, then create any missing
// model tables on existing Aspire volumes (EnsureCreated alone is a no-op when tables already
// exist — that made 147-006 principal_roles require hand DDL). Grown apps that need renames or
// column rewrites still switch to Database.Migrate (ADR-0009).
// Creates its own scope because IHostedService is a singleton while PostgresDbContext is scoped.
// Registered by PostgresDbModule so the behavior ships only when the postgres feature is wired in.
#endregion

namespace TimeWarp.Architecture.HostedServices;

using TimeWarp.Architecture.Persistence;

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
    await PostgresModelSchemaBootstrap
      .EnsureSchemaMatchesModelAsync(postgresDbContext, cancellationToken)
      .ConfigureAwait(false);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    LogStopped(Logger, null);
    return Task.CompletedTask;
  }
}
