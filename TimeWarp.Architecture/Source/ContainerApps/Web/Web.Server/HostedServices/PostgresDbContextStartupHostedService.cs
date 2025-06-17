//TODO - the is copilot generated code, it needs to be reviewed and cleaned up
namespace TimeWarp.Architecture.HostedServices;

public sealed partial class PostgresDbContextStartupHostedService : IHostedService
{
  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public PostgresDbContextStartupHostedService
  (
       IServiceProvider aServiceProvider,
          ILogger<PostgresDbContextStartupHostedService> aLogger
     )
  {
    ServiceProvider = aServiceProvider;
    Logger = aLogger;
  }

  public async Task StartAsync(CancellationToken aCancellationToken)
  {
    Logger.LogInformation($"{nameof(PostgresDbContextStartupHostedService)} has started.");
    using IServiceScope scope = ServiceProvider.CreateScope();

    PostgresDbContext postgresDbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
    await postgresDbContext.Database.EnsureCreatedAsync(aCancellationToken);
  }

  public Task StopAsync(CancellationToken aCancellationToken)
  {
    Logger.LogInformation($"{nameof(PostgresDbContextStartupHostedService)} has stopped.");
    return Task.CompletedTask;
  }
}
