#region Purpose
// Thin composition root for the Orleans ledger: builds a generic Host with the shared substrate + a
// localhost-clustered silo, and exposes DebitAsync (grain call) plus a measured startup duration.
// Shares ONE service provider with the caller so tests seed/read through the same DbContext factory
// and recording publisher.
#endregion

#region Design
// UseLocalhostClustering is the single-silo dev topology a template consumer starts with. No grain
// storage provider is registered (EF is the store). StartupDuration brackets host.StartAsync, where
// the silo boots (membership, grain directory, activation collector) — expected to cost more than
// Akka's ActorSystem boot, and that delta is one of the measured findings. DebitAsync maps the
// grain's LedgerPosted DTO back to the substrate's PostResult so the test asserts uniformly across
// candidates.
#endregion

namespace TimeWarp.Spike.DualActor.Orleans;

using System.Diagnostics;
using global::Orleans;
using global::Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class OrleansLedgerHost : IAsyncDisposable
{
  private readonly IHost host;
  private readonly IGrainFactory grainFactory;

  private OrleansLedgerHost(IHost host, IGrainFactory grainFactory, TimeSpan startupDuration)
  {
    this.host = host;
    this.grainFactory = grainFactory;
    StartupDuration = startupDuration;
  }

  public IServiceProvider Services => host.Services;

  public TimeSpan StartupDuration { get; }

  public static async Task<OrleansLedgerHost> StartAsync()
  {
    HostApplicationBuilder builder = Host.CreateApplicationBuilder();
    builder.Services.AddLedgerSubstrate();
    builder.UseOrleans(silo => silo.UseLocalhostClustering());

    IHost host = builder.Build();

    Stopwatch stopwatch = Stopwatch.StartNew();
    await host.StartAsync();
    stopwatch.Stop();

    IGrainFactory grainFactory = host.Services.GetRequiredService<IGrainFactory>();

    return new OrleansLedgerHost(host, grainFactory, stopwatch.Elapsed);
  }

  public async Task<PostResult> DebitAsync(PrincipalId id, long amount)
  {
    LedgerPosted posted = await grainFactory.GetGrain<ILedgerGrain>(id.Value).Debit(amount);
    return new PostResult(posted.Balance, posted.Version);
  }

  public async ValueTask DisposeAsync()
  {
    await host.StopAsync();
    host.Dispose();
  }
}
