#region Purpose
// Orleans concurrency demonstration: the SAME N parallel debits as the baseline, routed to one
// Guid-keyed grain. Orleans' turn-based concurrency serializes them, so ZERO optimistic-concurrency
// conflicts (a conflict would fault a grain call and fail the test), balance exact, Version == N, and
// N events recorded through the substrate-agnostic publish seam.
#endregion

#region Design
// Same structural "zero conflicts" assertion as the Akka test: the grain does not catch/retry
// DbUpdateConcurrencyException, so a serialization failure would surface here. Version == N confirms
// one Modified save per debit with no wasted attempts. Drives the grain via IGrainFactory with a
// single localhost silo and NO external cluster scaffolding — the "testability under Fixie" criterion.
#endregion

namespace TimeWarp.Spike.DualActor.Tests;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Spike.DualActor.Orleans;

public class OrleansConcurrency
{
  public async Task Parallel_debits_serialize_through_the_grain_with_zero_conflicts()
  {
    await using OrleansLedgerHost host = await OrleansLedgerHost.StartAsync();
    await SpikePostgres.EnsureFreshSchemaAsync(host.Services);

    PrincipalId id = PrincipalId.New();
    await LedgerScenario.SeedAsync(host.Services, id);

    Task<PostResult>[] commands = Enumerable.Range(0, LedgerScenario.ParallelCommands)
      .Select(_ => host.DebitAsync(id, LedgerScenario.DebitAmount))
      .ToArray();

    await Task.WhenAll(commands);

    (long balance, long version) = await LedgerScenario.ReadAsync(host.Services, id);
    RecordingIntegrationEventPublisher publisher = host.Services.GetRequiredService<RecordingIntegrationEventPublisher>();
    int events = publisher.CountOf<LedgerEntryPosted>();

    Console.WriteLine(
      $"[orleans] N={LedgerScenario.ParallelCommands} parallel debits: startup={host.StartupDuration.TotalMilliseconds:F0}ms, " +
      $"conflicts=0, finalBalance={balance}, version={version}, events={events}");

    balance.ShouldBe(LedgerScenario.ExpectedFinalBalance);
    version.ShouldBe(LedgerScenario.ExpectedFinalVersion);
    events.ShouldBe(LedgerScenario.ParallelCommands);
  }
}
