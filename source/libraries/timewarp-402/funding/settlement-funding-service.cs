#region Purpose
// Identity ↔ 402 composition: after a successful settle, credit the ledger and promote to Funded.
#endregion

#region Design
// Shared settle→fund hook (task 104-013). PaymentGate stays host-agnostic verify/settle only;
// MeteredCapabilityGate (and any future paid surface with a PrincipalId) calls this after
// PaymentSettled so credit + TrustTier stay in one place rather than forking per endpoint.
//
// Policy:
// 1. CreditAsync(amount, receiptId) — always, idempotent by receipt (facilitator settle retries
//    must not double-fund). Ledger does not require a principal row; credit is economic fact.
// 2. Promote to TrustTier.Funded when the principal exists, is not quarantined, and is strictly
//    below Funded (Provisional or Keyed). Promote allows any strictly higher tier, so Provisional
//    may jump to Funded in one step (payment without prior credential attach is allowed for agents
//    that paid — product decision "no human required if the agent pays").
// 3. Already Funded or Established → no-op on tier (return PromotedToFunded=false).
// 4. Quarantined → credit only; do not call Promote (quarantine is ops risk control and orthogonal
//    to economic funding; IsFundedAndActive stays false until ClearQuarantine).
// 5. Missing principal → credit only (best-effort tier; never fail after chain settle confirmed).
// 6. ConcurrencyConflictException on UpdatePrincipal → re-get once; if already Funded+ treat as
//    success (no promotion this call); else retry Promote+Update once more then give up (credit
//    already applied; tier lag is healable on next settle).
//
// Debit does NOT demote TrustTier. Funded means "has successfully settled at least once" (or was
// ops-promoted), not "currently has positive balance." Metered debit / zero balance leave the
// principal Funded; expensive capability gates should check balance and/or IsFundedAndActive
// separately. Immediate demotion on empty balance would re-open Sybil free tiers after one use.
//
// Lifetime: depends on IPrincipalStore (scoped under EF/postgres; singleton in-memory). Register
// this service as scoped (or match the principal store lifetime) — do not capture a scoped store
// into a singleton PaymentGate/MeteredCapabilityGate without a scope factory.
#endregion

namespace TimeWarp.X402;

using TimeWarp.Identity;

/// <summary>
/// Applies settlement economic effects: ledger credit + optional TrustTier.Funded promotion.
/// </summary>
public sealed class SettlementFundingService
{
  private readonly ICreditLedger Ledger;
  private readonly IPrincipalStore Principals;

  public SettlementFundingService(ICreditLedger ledger, IPrincipalStore principals)
  {
    Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    Principals = principals ?? throw new ArgumentNullException(nameof(principals));
  }

  /// <summary>
  /// Credits <paramref name="amount"/> under <paramref name="receiptId"/> and promotes the
  /// principal to <see cref="TrustTier.Funded"/> when policy allows.
  /// </summary>
  public async Task<SettlementFundingResult> ApplyAsync(
    PrincipalId principalId,
    decimal amount,
    string receiptId,
    CancellationToken cancellationToken = default)
  {
    if (principalId.IsEmpty)
    {
      throw new ArgumentException("PrincipalId must not be empty.", nameof(principalId));
    }

    ArgumentException.ThrowIfNullOrWhiteSpace(receiptId);

    decimal balance = await Ledger
      .CreditAsync(principalId, amount, receiptId, cancellationToken)
      .ConfigureAwait(false);

    bool promoted = await TryPromoteToFundedAsync(principalId, cancellationToken)
      .ConfigureAwait(false);

    return new SettlementFundingResult(balance, promoted);
  }

  private async Task<bool> TryPromoteToFundedAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken)
  {
    for (int attempt = 0; attempt < 2; attempt++)
    {
      Principal? principal = await Principals
        .GetPrincipalAsync(principalId, cancellationToken)
        .ConfigureAwait(false);

      if (principal is null)
      {
        return false;
      }

      if (principal.TrustTier is TrustTier.Funded or TrustTier.Established)
      {
        return false;
      }

      if (principal.IsQuarantined)
      {
        return false;
      }

      // Provisional or Keyed (and any future below-Funded progression value).
      if ((int)principal.TrustTier >= (int)TrustTier.Funded)
      {
        return false;
      }

      principal.Promote(TrustTier.Funded);

      try
      {
        await Principals
          .UpdatePrincipalAsync(principal, cancellationToken)
          .ConfigureAwait(false);
        return true;
      }
      catch (ConcurrencyConflictException) when (attempt == 0)
      {
        // Concurrent writer updated the principal; retry once with a fresh snapshot.
      }
      catch (ConcurrencyConflictException)
      {
        Principal? after = await Principals
          .GetPrincipalAsync(principalId, cancellationToken)
          .ConfigureAwait(false);
        if (after is not null &&
            after.TrustTier is TrustTier.Funded or TrustTier.Established)
        {
          return false;
        }

        // Credit already applied; leave tier for next settle or ops — do not fail the payment.
        return false;
      }
    }

    return false;
  }
}
