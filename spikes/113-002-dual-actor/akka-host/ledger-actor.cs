#region Purpose
// The Akka single-writer: one LedgerActor per principal. Its mailbox serializes all commands for
// that principal, so concurrent debits queue instead of racing the EF store. Loads the aggregate on
// first message into a long-lived DbContext, mutates in memory, SaveChanges per command, publishes
// one integration event through the substrate-agnostic seam.
#endregion

#region Design
// ReceiveAsync (not Receive): Akka suspends this actor's mailbox until the awaited handler completes,
// so even though the body awaits EF I/O, the NEXT Debit is not dequeued until this one has saved and
// replied — that suspension IS the single-writer guarantee the concurrency test relies on.
// Long-lived DbContext held for the actor's lifetime (created lazily on first command, disposed in
// PostStop): legitimate here precisely because the actor is the sole writer, so the usual
// "short-lived context" rule does not apply. After each SaveChanges, EF advances the tracked
// entity's original Version, so the next save's concurrency check is against the fresh value — and
// since no other writer exists for this principal, the check never trips (zero conflicts, vs the
// baseline's hundreds). A production actor would also idle-passivate; out of scope for the spike.
// The aggregate + EF + publish seam are all substrate code (ledger-substrate); this file is pure
// Akka glue. Nothing here is EF- or domain-specific beyond the two lines that call the aggregate.
#endregion

namespace TimeWarp.Spike.DualActor.Akka;

using global::Akka.Actor;
using Microsoft.EntityFrameworkCore;

public sealed class LedgerActor : ReceiveActor
{
  private readonly PrincipalId id;
  private readonly IDbContextFactory<LedgerDbContext> contextFactory;
  private readonly IIntegrationEventPublisher publisher;

  private LedgerDbContext? context;
  private Ledger? ledger;

  public LedgerActor(PrincipalId id, IDbContextFactory<LedgerDbContext> contextFactory, IIntegrationEventPublisher publisher)
  {
    this.id = id;
    this.contextFactory = contextFactory;
    this.publisher = publisher;

    ReceiveAsync<LedgerMessages.Debit>(HandleDebitAsync);
  }

  private async Task HandleDebitAsync(LedgerMessages.Debit command)
  {
    if (context is null || ledger is null)
    {
      context = await contextFactory.CreateDbContextAsync();
      ledger = await context.Ledgers.FindAsync(id)
        ?? throw new InvalidOperationException($"Ledger {id} is not open.");
    }

    ledger.Debit(command.Amount);
    await context.SaveChangesAsync();

    await publisher.PublishAsync(new LedgerEntryPosted(id, -command.Amount, ledger.Balance, ledger.Version));

    Sender.Tell(new PostResult(ledger.Balance, ledger.Version));
  }

  protected override void PostStop()
  {
    context?.Dispose();
    base.PostStop();
  }
}
