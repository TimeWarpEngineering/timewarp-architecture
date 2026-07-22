#region Purpose
// Akka concurrency demonstration: the SAME N parallel debits as the baseline, but routed through one
// per-principal actor. The mailbox serializes them, so there are ZERO optimistic-concurrency
// conflicts (any conflict would fault an Ask and fail the test), the balance is exact, Version lands
// at exactly N, and N events are recorded through the substrate-agnostic publish seam.
#endregion

#region Design
// "Zero conflicts" is asserted structurally: the actor does not catch/retry DbUpdateConcurrencyException,
// so if serialization failed the faulted Ask would surface here as a test failure. Version == N
// (exactly one Modified save per debit, no wasted attempts) is the positive confirmation that no
// command was retried. The test drives the actor via Ask with NO cluster scaffolding — answering the
// "testability under Fixie" criterion directly.
#endregion

namespace TimeWarp.Spike.DualActor.Tests;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Spike.DualActor.Akka;

public class AkkaConcurrency
{
  public async Task Parallel_debits_serialize_through_the_actor_with_zero_conflicts()
  {
    await using AkkaLedgerHost host = await AkkaLedgerHost.StartAsync();
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
      $"[akka] N={LedgerScenario.ParallelCommands} parallel debits: startup={host.StartupDuration.TotalMilliseconds:F0}ms, " +
      $"conflicts=0, finalBalance={balance}, version={version}, events={events}");

    balance.ShouldBe(LedgerScenario.ExpectedFinalBalance);
    version.ShouldBe(LedgerScenario.ExpectedFinalVersion);
    events.ShouldBe(LedgerScenario.ParallelCommands);
  }
}
