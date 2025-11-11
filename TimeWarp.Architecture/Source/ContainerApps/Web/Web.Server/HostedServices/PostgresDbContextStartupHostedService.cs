//TODO - the is copilot generated code, it needs to be reviewed and cleaned up
namespace TimeWarp.Architecture.HostedServices;

public sealed partial class PostgresDbContextStartupHostedService : IHostedService
{
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
    Logger.LogInformation($"{nameof(PostgresDbContextStartupHostedService)} has started.");
    using IServiceScope scope = ServiceProvider.CreateScope();

    PostgresDbContext postgresDbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
    await postgresDbContext.Database.EnsureCreatedAsync(cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.LogInformation($"{nameof(PostgresDbContextStartupHostedService)} has stopped.");
    return Task.CompletedTask;
  }
}
