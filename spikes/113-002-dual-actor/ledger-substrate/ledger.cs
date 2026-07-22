#region Purpose
// Credit-ledger aggregate: a per-principal balance with credit/debit commands and a
// never-overdraw invariant — the textbook high-contention single-writer this spike hosts on both
// Akka and Orleans.
#endregion

#region Design
// Golden pattern, identical shape to source/container-apps/web/web-domain/aggregates/profile/
// profile.cs: sealed Entity<TId> + IAggregateRoot; private ctor; fail-closed static factory (Open);
// intention-revealing named mutations (Credit/Debit) with no public setters; nested PRIVATE
// Invariants : AbstractValidator<Ledger> (TWA0011/0012 shape — analyzer not attached in the spike
// tree, so the shape is honored by hand). Balance is minor units (long, e.g. cents) to keep
// arithmetic exact and the concurrency test's "balance is exact" assertion unambiguous.
// The overdraw rule lives in BOTH the Debit guard clause (so an invalid state never even forms in
// memory) AND the nested Invariants validator (the save-time backstop DomainInvariantsGuard runs
// from the DbContext hook), deliberately not validator-only — the same belt-and-suspenders the
// Profile exemplar uses against agreement-by-memory drift.
// Version (the store-owned optimistic-concurrency token) comes from Entity<TId>; the EF hook in
// LedgerDbContext increments it and LedgerDbContext maps it .IsConcurrencyToken(). This aggregate
// knows nothing about actors, grains, or EF — that ignorance is the point.
#endregion

namespace TimeWarp.Spike.DualActor;

using FluentValidation;
using TimeWarp.Foundation.Entities;

public sealed class Ledger : Entity<PrincipalId>, IAggregateRoot
{
  private Ledger(PrincipalId id, long balance) : base(id)
  {
    Balance = balance;
  }

  public long Balance { get; private set; }

  public static Ledger Open(PrincipalId id)
  {
    ArgumentOutOfRangeException.ThrowIfEqual(id.IsEmpty, true, nameof(id));
    return new Ledger(id, 0);
  }

  public void Credit(long amount)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
    Balance += amount;
  }

  public void Debit(long amount)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
    if (amount > Balance)
    {
      throw new InvalidOperationException(
        $"Debit of {amount} would overdraw ledger {Id} (balance {Balance}).");
    }

    Balance -= amount;
  }

  private sealed class Invariants : AbstractValidator<Ledger>
  {
    public Invariants()
    {
      RuleFor(ledger => ledger.Balance).GreaterThanOrEqualTo(0);
    }
  }
}
