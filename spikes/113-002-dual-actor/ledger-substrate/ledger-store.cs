#region Purpose
// Thin per-command EF store/loader for the ledger. This is the PLAIN-EF path: every call uses a
// fresh short-lived DbContext (load, mutate, save), the way stateless parallel request handlers do.
// The baseline concurrency test drives this directly; the actor/grain hosts do NOT use it (they own
// their own long-lived context so a single writer serializes commands).
#endregion

#region Design
// DebitAsync deliberately does NOT retry: it surfaces DbUpdateConcurrencyException to the caller so
// the baseline test can count conflicts. The retrying variant lives in the test harness (so the
// retry count is observable). Publishing happens only after a successful save, so recorded-event
// count equals successful commands, not attempts.
#endregion

namespace TimeWarp.Spike.DualActor;

using Microsoft.EntityFrameworkCore;

public readonly record struct PostResult(long NewBalance, long Version);

public sealed class LedgerStore
{
  private readonly IDbContextFactory<LedgerDbContext> contextFactory;
  private readonly IIntegrationEventPublisher publisher;

  public LedgerStore(IDbContextFactory<LedgerDbContext> contextFactory, IIntegrationEventPublisher publisher)
  {
    this.contextFactory = contextFactory;
    this.publisher = publisher;
  }

  public async Task OpenAsync(PrincipalId id, CancellationToken cancellationToken = default)
  {
    await using LedgerDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
    Ledger? existing = await context.Ledgers.FindAsync([id], cancellationToken);
    if (existing is not null) return;

    context.Ledgers.Add(Ledger.Open(id));
    await context.SaveChangesAsync(cancellationToken);
  }

  public async Task<PostResult> DebitAsync(PrincipalId id, long amount, CancellationToken cancellationToken = default)
  {
    await using LedgerDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
    Ledger ledger = await context.Ledgers.FindAsync([id], cancellationToken)
      ?? throw new InvalidOperationException($"Ledger {id} is not open.");

    ledger.Debit(amount);
    await context.SaveChangesAsync(cancellationToken); // may throw DbUpdateConcurrencyException

    await publisher.PublishAsync(
      new LedgerEntryPosted(id, -amount, ledger.Balance, ledger.Version), cancellationToken);

    return new PostResult(ledger.Balance, ledger.Version);
  }
}
