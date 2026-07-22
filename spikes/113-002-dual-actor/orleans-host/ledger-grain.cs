#region Purpose
// The Orleans single-writer: one LedgerGrain activation per principal. Orleans' turn-based (non-
// reentrant) concurrency serializes all calls to a grain, so concurrent debits queue instead of
// racing the EF store. Loads the aggregate on activation into a long-lived DbContext, mutates in
// memory, SaveChanges per call, publishes one integration event through the substrate-agnostic seam.
#endregion

#region Design
// OnActivateAsync is a REAL async activation hook (contrast Akka, which has no async PreStart, so the
// actor lazy-loads on first message) — the grain loads its aggregate eagerly before the first Debit.
// Turn-based concurrency: Orleans will not begin the next Debit turn until this one's awaited task
// (including SaveChangesAsync) completes, so the long-lived DbContext is only ever touched by one
// turn at a time — that IS the zero-conflict guarantee. The grain key (Guid) is the principal id.
// The grain depends only on the substrate seams (IDbContextFactory, IIntegrationEventPublisher) +
// the aggregate; the sole Orleans-shaped concession is mapping the result into the LedgerPosted DTO
// (see i-ledger-grain.cs). No Orleans grain-storage/[PersistentState] is used — EF is the store.
#endregion

namespace TimeWarp.Spike.DualActor.Orleans;

using global::Orleans;
using Microsoft.EntityFrameworkCore;

public sealed class LedgerGrain : Grain, ILedgerGrain
{
  private readonly IDbContextFactory<LedgerDbContext> contextFactory;
  private readonly IIntegrationEventPublisher publisher;

  private PrincipalId id;
  private LedgerDbContext? context;
  private Ledger? ledger;

  public LedgerGrain(IDbContextFactory<LedgerDbContext> contextFactory, IIntegrationEventPublisher publisher)
  {
    this.contextFactory = contextFactory;
    this.publisher = publisher;
  }

  public override async Task OnActivateAsync(CancellationToken cancellationToken)
  {
    id = new PrincipalId(this.GetPrimaryKey());
    context = await contextFactory.CreateDbContextAsync(cancellationToken);
    ledger = await context.Ledgers.FindAsync([id], cancellationToken)
      ?? throw new InvalidOperationException($"Ledger {id} is not open.");
    await base.OnActivateAsync(cancellationToken);
  }

  public async Task<LedgerPosted> Debit(long amount)
  {
    ledger!.Debit(amount);
    await context!.SaveChangesAsync();

    await publisher.PublishAsync(new LedgerEntryPosted(id, -amount, ledger.Balance, ledger.Version));

    return new LedgerPosted(ledger.Balance, ledger.Version);
  }

  public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
  {
    if (context is not null)
    {
      await context.DisposeAsync();
    }

    await base.OnDeactivateAsync(reason, cancellationToken);
  }
}
