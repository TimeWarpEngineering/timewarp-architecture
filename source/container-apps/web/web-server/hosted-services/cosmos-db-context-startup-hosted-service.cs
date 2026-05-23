namespace TimeWarp.Architecture.HostedServices;

public class CosmosDbContextStartupHostedService : IHostedService
{
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
    Logger.LogInformation($"{nameof(CosmosDbContextStartupHostedService)} has started.");
    using IServiceScope scope = ServiceProvider.CreateScope();

    CosmosDbContext cosmosDbContext = scope.ServiceProvider.GetRequiredService<CosmosDbContext>();
    await cosmosDbContext.Database.EnsureCreatedAsync(cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.LogInformation($"{nameof(CosmosDbContextStartupHostedService)} has stopped.");
    return Task.CompletedTask;
  }
}
