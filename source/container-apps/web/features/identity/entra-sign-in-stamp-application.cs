#region Purpose
// Stamps last-used on an Entra credential at sign-in and refreshes its account hint in the same write.
#endregion

#region Design
// Credential.Label is the provider, EntraIdTokenClaims.ProviderLabel ("Microsoft 365"), on both
// bootstrap and link Create; the account is Credential.AccountHint = the token's preferred_username
// (task 250 — display-only, never a join key; the join stays tid+oid). Principal.SetDisplayName stays
// the name claim. Refresh decision (task 250): a sign-in that resolves to an existing active Entra
// credential (sync-hit, and the already-linked early return of CompleteBootstrapCreateAsync) also
// refreshes AccountHint when the token carries a different preferred_username — that backfills links
// made before 250 (which have no hint) and follows UPN renames. A token WITHOUT preferred_username
// leaves the stored hint alone (absence is not evidence the account changed). The refresh is
// advisory like last-used and is persisted BY the last-used stamp write (task 252, below): a lost
// Version race is DROPPED — a concurrent revoke/rename wins and the next sign-in retries. The
// hint is PII: it is never logged here and reaches the wire only via the caller's own GetCredentials.
// Last-used (task 252): every successful Entra sign-in stamps Credential.LastUsedAt through
// CredentialUsageRecorder.RecordAsync — the same per-ceremony write as passkey sign-in and agent-token
// issuance (248-002). Stamp points: sync-hit, the already-linked early return of
// CompleteBootstrapCreateAsync, the bootstrap-create race winner, and every link that lands a
// credential (new link, bootstrap mint, already-have attach, and link-merge of a foreign owner — the
// merged row is re-read first because MergePrincipalAsync re-parented and re-versioned it). Each stamp
// runs AFTER the principal liveness/quarantine check of its path, so a quarantined principal never
// stamps. One UPDATE per sign-in: the AccountHint refresh is applied to the same snapshot in memory
// (SetAccountHint) and persisted by the recorder's stamp write, so hint + stamp land in one Version
// bump. Consequently every sign-in writes once (the stamp always advances), not only when the hint
// changed. Races follow the recorder's rule: a lost Version race is dropped, never retried, for both
// fields — revoke wins; the next sign-in retries. Edge: if the stamp does not advance (the clock
// regressed below the stored instant) the recorder skips the write and a changed hint waits for the
// next sign-in — advisory display state, not worth a second write. A new link is INSERT (Add) then
// the stamp UPDATE (RecordNewAsync): the recorder owns the clock and the race rule, so Create does
// not pre-stamp.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

/// <summary>The last-used stamp (and account hint refresh) for Entra sign-ins.</summary>
internal sealed class EntraSignInStamp
{
  private readonly IPrincipalStore PrincipalStore;
  private readonly CredentialUsageRecorder UsageRecorder;

  public EntraSignInStamp(IPrincipalStore principalStore, CredentialUsageRecorder usageRecorder)
  {
    PrincipalStore = principalStore;
    UsageRecorder = usageRecorder;
  }

  /// <summary>Stamps an existing credential and applies the token's account hint when it has one.</summary>
  public async Task RecordSignInAsync(
    Credential existing,
    EntraIdTokenClaims claims,
    CancellationToken cancellationToken)
  {
    // Hint refresh rides the stamp write (one UPDATE); an absent claim keeps the stored hint.
    if (claims.AccountHint is not null)
    {
      existing.SetAccountHint(claims.AccountHint);
    }

    // Advisory: a lost Version race is dropped inside the recorder, never retried (Design region).
    await UsageRecorder.RecordAsync(PrincipalStore, existing, cancellationToken);
  }

  /// <summary>Stamps a credential this sign-in just inserted. Its hint came from Create.</summary>
  public Task RecordNewAsync(Credential created, CancellationToken cancellationToken) =>
    UsageRecorder.RecordAsync(PrincipalStore, created, cancellationToken);
}
