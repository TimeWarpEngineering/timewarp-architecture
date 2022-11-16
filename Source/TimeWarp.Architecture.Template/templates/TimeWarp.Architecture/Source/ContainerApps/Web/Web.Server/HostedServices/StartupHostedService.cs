namespace TimeWarp.Architecture.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture.Data;

public class StartupHostedService : IHostedService
{
  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public StartupHostedService
  (
    IServiceProvider aServiceProvider,
    ILogger<StartupHostedService> aLogger
  )
  {
    ServiceProvider = aServiceProvider;
    Logger = aLogger;
  }

  public Task StartAsync(CancellationToken aCancellationToken)
  {
    Logger.LogInformation($"{nameof(StartupHostedService)} has started.");
    using IServiceScope scope = ServiceProvider.CreateScope();

    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken aCancellationToken)
  {
    Logger.LogInformation($"{nameof(StartupHostedService)} has stopped.");
    return Task.CompletedTask;
  }
}
