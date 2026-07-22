#region Purpose
// Shared scenario constants + helpers for the three concurrency demonstrations (baseline, Akka,
// Orleans): fixed N, seed balance, debit amount, fresh-schema/seed/read glue so every candidate
// runs the identical workload and the outputs are comparable.
#endregion

namespace TimeWarp.Spike.DualActor.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static class LedgerScenario
{
  public const int ParallelCommands = 50;
  public const long SeedBalance = 1000;
  public const long DebitAmount = 1;
  public const long ExpectedFinalBalance = SeedBalance - (ParallelCommands * DebitAmount);
  public const long ExpectedFinalVersion = ParallelCommands; // one Modified save per successful debit

  public static async Task SeedAsync(IServiceProvider services, PrincipalId id)
  {
    IDbContextFactory<LedgerDbContext> factory = services.GetRequiredService<IDbContextFactory<LedgerDbContext>>();
    await using LedgerDbContext context = await factory.CreateDbContextAsync();
    Ledger ledger = Ledger.Open(id);
    ledger.Credit(SeedBalance);
    context.Ledgers.Add(ledger);
    await context.SaveChangesAsync();
  }

  public static async Task<(long Balance, long Version)> ReadAsync(IServiceProvider services, PrincipalId id)
  {
    IDbContextFactory<LedgerDbContext> factory = services.GetRequiredService<IDbContextFactory<LedgerDbContext>>();
    await using LedgerDbContext context = await factory.CreateDbContextAsync();
    Ledger ledger = await context.Ledgers.FindAsync([id])
      ?? throw new InvalidOperationException($"Ledger {id} not found.");
    return (ledger.Balance, ledger.Version);
  }
}
