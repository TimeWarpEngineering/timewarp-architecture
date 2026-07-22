#region Purpose
// Thin composition root a template consumer would write to stand up the Akka ledger: builds a
// generic Host with the shared substrate + an ActorSystem (via Akka.Hosting), spawns the
// coordinator, and exposes DebitAsync (Ask) plus a measured startup duration. Shares ONE service
// provider with the caller so tests seed/read through the same DbContext factory and recording
// publisher.
#endregion

#region Design
// Uses Host + Akka.Hosting's AddAkka/WithActors — the idiomatic hosting integration, so the ceremony
// counted here is what a real consumer pays, not a hand-rolled ActorSystem. StartupDuration brackets
// host.StartAsync (where Akka.Hosting's hosted service actually boots the ActorSystem). No
// Persistence/Cluster/Sharding modules are added.
#endregion

namespace TimeWarp.Spike.DualActor.Akka;

using System.Diagnostics;
using global::Akka.Actor;
using global::Akka.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class AkkaLedgerHost : IAsyncDisposable
{
  private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(30);

  private readonly IHost host;
  private readonly IActorRef coordinator;

  private AkkaLedgerHost(IHost host, IActorRef coordinator, TimeSpan startupDuration)
  {
    this.host = host;
    this.coordinator = coordinator;
    StartupDuration = startupDuration;
  }

  public IServiceProvider Services => host.Services;

  public TimeSpan StartupDuration { get; }

  public static async Task<AkkaLedgerHost> StartAsync()
  {
    HostApplicationBuilder builder = Host.CreateApplicationBuilder();
    builder.Services.AddLedgerSubstrate();

    builder.Services.AddAkka("LedgerSpikeSystem", (configuration, serviceProvider) =>
    {
      configuration.WithActors((system, registry) =>
      {
        IDbContextFactory<LedgerDbContext> factory = serviceProvider.GetRequiredService<IDbContextFactory<LedgerDbContext>>();
        IIntegrationEventPublisher publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();
        IActorRef coordinator = system.ActorOf(
          Props.Create(() => new LedgerCoordinator(factory, publisher)), "ledger-coordinator");
        registry.Register<LedgerCoordinator>(coordinator);
      });
    });

    IHost host = builder.Build();

    Stopwatch stopwatch = Stopwatch.StartNew();
    await host.StartAsync();
    stopwatch.Stop();

    ActorRegistry registry = host.Services.GetRequiredService<ActorRegistry>();
    IActorRef coordinator = registry.Get<LedgerCoordinator>();

    return new AkkaLedgerHost(host, coordinator, stopwatch.Elapsed);
  }

  public Task<PostResult> DebitAsync(PrincipalId id, long amount) =>
    coordinator.Ask<PostResult>(new LedgerMessages.Debit(id, amount), AskTimeout);

  public async ValueTask DisposeAsync()
  {
    await host.StopAsync();
    host.Dispose();
  }
}
