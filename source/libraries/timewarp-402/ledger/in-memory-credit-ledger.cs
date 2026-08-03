#region Purpose
// Thread-safe in-memory credit ledger for tests, demos, and single-process hosts.
#endregion

#region Design
// Per-principal lock + global receipt set for idempotency. Receipt ids are stored forever in this
// process (acceptable for demos; a durable store would key receipts by (principal, receiptId)).
// Debit operationId is optional bookkeeping only in-memory — not required for correctness here.
// Snapshot semantics: returned balances are value copies (decimal); no shared mutable balance object.
#endregion

namespace TimeWarp.X402;
using System.Collections.Concurrent;
using TimeWarp.Identity;

/// <summary>In-memory <see cref="ICreditLedger"/> implementation.</summary>
public sealed class InMemoryCreditLedger : ICreditLedger
{
  private readonly ConcurrentDictionary<PrincipalId, decimal> Balances = new();
  private readonly ConcurrentDictionary<string, byte> AppliedReceipts = new(StringComparer.Ordinal);
  private readonly ConcurrentDictionary<PrincipalId, object> Locks = new();

  public Task<decimal> CreditAsync(
    PrincipalId principalId,
    decimal amount,
    string receiptId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    ArgumentException.ThrowIfNullOrWhiteSpace(receiptId);
    if (amount <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), amount, "Credit amount must be positive.");
    }

    if (principalId.IsEmpty)
    {
      throw new ArgumentException("PrincipalId must not be empty.", nameof(principalId));
    }

    object gate = Locks.GetOrAdd(principalId, static _ => new object());
    lock (gate)
    {
      if (!AppliedReceipts.TryAdd(receiptId, 0))
      {
        return Task.FromResult(Balances.GetValueOrDefault(principalId));
      }

      decimal next = Balances.AddOrUpdate(principalId, amount, (_, current) => current + amount);
      return Task.FromResult(next);
    }
  }

  public Task<decimal> DebitAsync(
    PrincipalId principalId,
    decimal amount,
    string? operationId = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (amount <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), amount, "Debit amount must be positive.");
    }

    if (principalId.IsEmpty)
    {
      throw new ArgumentException("PrincipalId must not be empty.", nameof(principalId));
    }

    object gate = Locks.GetOrAdd(principalId, static _ => new object());
    lock (gate)
    {
      decimal current = Balances.GetValueOrDefault(principalId);
      if (current < amount)
      {
        throw new InsufficientCreditException(principalId, amount, current);
      }

      decimal next = current - amount;
      Balances[principalId] = next;
      _ = operationId; // reserved for durable audit later
      return Task.FromResult(next);
    }
  }

  public Task<decimal> GetBalanceAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(Balances.GetValueOrDefault(principalId));
  }
}
