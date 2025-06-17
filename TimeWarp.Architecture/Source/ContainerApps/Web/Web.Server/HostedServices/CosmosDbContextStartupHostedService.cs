namespace TimeWarp.Architecture.HostedServices;

public class CosmosDbContextStartupHostedService : IHostedService
{
  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public CosmosDbContextStartupHostedService
  (
    IServiceProvider aServiceProvider,
    ILogger<CosmosDbContextStartupHostedService> aLogger
  )
  {
    ServiceProvider = aServiceProvider;
    Logger = aLogger;
  }

  public async Task StartAsync(CancellationToken aCancellationToken)
  {
    Logger.LogInformation($"{nameof(CosmosDbContextStartupHostedService)} has started.");
    using IServiceScope scope = ServiceProvider.CreateScope();

    CosmosDbContext cosmosDbContext = scope.ServiceProvider.GetRequiredService<CosmosDbContext>();
    await cosmosDbContext.Database.EnsureCreatedAsync(aCancellationToken);
  }

  public Task StopAsync(CancellationToken aCancellationToken)
  {
    Logger.LogInformation($"{nameof(CosmosDbContextStartupHostedService)} has stopped.");
    return Task.CompletedTask;
  }
}
