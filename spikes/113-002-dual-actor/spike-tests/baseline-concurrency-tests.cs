#region Purpose
// The PLAIN-EF baseline concurrency demonstration: N parallel conflicting debits against ONE
// principal with no actor/grain in front of the store. Proves the optimistic-concurrency token
// actually fires (>= 1 DbUpdateConcurrencyException) and reports the retry count needed to reach a
// correct final balance — the "problem" the actor/grain candidates are meant to remove.
#endregion

#region Design
// Each parallel worker retries on DbUpdateConcurrencyException until its debit commits, counting
// conflicts globally (Interlocked). A stale writer's UPDATE matches zero rows (WHERE Version =
// @original) and throws; the retry reloads the fresh version and tries again. Final balance/version
// are still exact because every debit eventually commits — the difference from the actor/grain runs
// is purely the conflict count (baseline > 0; actor/grain == 0). Requires the ephemeral Postgres on
// 5433 (or SPIKE_POSTGRES_CONNECTION); EnsureFreshSchema per run.
#endregion

namespace TimeWarp.Spike.DualActor.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class BaselineConcurrency
{
  public async Task Parallel_debits_provoke_conflicts_but_reach_exact_balance()
  {
    ServiceProvider services = new ServiceCollection().AddLedgerSubstrate().BuildServiceProvider();
    await SpikePostgres.EnsureFreshSchemaAsync(services);

    PrincipalId id = PrincipalId.New();
    await LedgerScenario.SeedAsync(services, id);

    LedgerStore store = services.GetRequiredService<LedgerStore>();
    int conflicts = 0;

    Task[] workers = Enumerable.Range(0, LedgerScenario.ParallelCommands)
      .Select(_ => Task.Run(async () =>
      {
        while (true)
        {
          try
          {
            await store.DebitAsync(id, LedgerScenario.DebitAmount);
            return;
          }
          catch (DbUpdateConcurrencyException)
          {
            Interlocked.Increment(ref conflicts);
          }
        }
      }))
      .ToArray();

    await Task.WhenAll(workers);

    (long balance, long version) = await LedgerScenario.ReadAsync(services, id);
    RecordingIntegrationEventPublisher publisher = services.GetRequiredService<RecordingIntegrationEventPublisher>();
    int events = publisher.CountOf<LedgerEntryPosted>();

    Console.WriteLine(
      $"[baseline] N={LedgerScenario.ParallelCommands} parallel debits: conflicts(retries)={conflicts}, " +
      $"finalBalance={balance}, version={version}, events={events}");

    conflicts.ShouldBeGreaterThanOrEqualTo(1);
    balance.ShouldBe(LedgerScenario.ExpectedFinalBalance);
    version.ShouldBe(LedgerScenario.ExpectedFinalVersion);
    events.ShouldBe(LedgerScenario.ParallelCommands);

    await services.DisposeAsync();
  }
}
