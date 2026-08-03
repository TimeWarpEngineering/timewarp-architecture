#region Purpose
// Port for principal-keyed credit balances: credit on settle, debit on metered use, query balance.
#endregion

#region Design
// Economic limiter for agents (task 104-010). Bound to TimeWarp.Identity.PrincipalId — no human
// required. Credit application is idempotent by receipt id so facilitator settle retries do not
// double-fund. Debit fails closed on insufficient balance (no silent negative). Hosts may swap
// in-memory for EF later; this package ships InMemoryCreditLedger for demos and tests.
// Amounts are decimal major units of the account currency (typically USD for $ prices). Atomicity
// of multi-step host workflows (settle then credit) is the host's responsibility until a unit-of-work
// seam exists.
#endregion

namespace TimeWarp.X402;
using TimeWarp.Identity;

/// <summary>Credit ledger keyed by principal.</summary>
public interface ICreditLedger
{
  /// <summary>
  /// Credits <paramref name="amount"/> to <paramref name="principalId"/> if
  /// <paramref name="receiptId"/> has not been applied. Replays of the same receipt are no-ops
  /// that return the current balance (idempotent settle application).
  /// </summary>
  /// <returns>Balance after the operation.</returns>
  Task<decimal> CreditAsync(
    PrincipalId principalId,
    decimal amount,
    string receiptId,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Debits <paramref name="amount"/> if balance is sufficient. Throws
  /// <see cref="InsufficientCreditException"/> otherwise.
  /// </summary>
  /// <returns>Balance after the debit.</returns>
  Task<decimal> DebitAsync(
    PrincipalId principalId,
    decimal amount,
    string? operationId = null,
    CancellationToken cancellationToken = default);

  /// <summary>Current balance for the principal (0 when never credited).</summary>
  Task<decimal> GetBalanceAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default);
}
