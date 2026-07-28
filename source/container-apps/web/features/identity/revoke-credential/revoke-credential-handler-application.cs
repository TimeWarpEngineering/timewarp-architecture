#region Purpose
// Server-side handler for the RevokeCredential command: soft-revokes one of the CALLER's own
// credentials, under a bounded optimistic-concurrency retry loop.
#endregion

#region Design
// IDOR rule (task 104-005, load-bearing, security): the caller's principal id comes ONLY from
// ICurrentPrincipalAccessor. The target credential is looked up by CredentialId (from the route), and
// ownership is checked (credential.PrincipalId == callerId) BEFORE any mutation. An unknown
// CredentialId and a credential that belongs to a DIFFERENT principal return the EXACT SAME 404 — 403
// would leak "this id exists, you just can't touch it," which is an enumeration oracle for other
// principals' credential ids. Same reasoning as the no-enumeration-oracle posture documented on
// CompleteAgentTokenIssuance.Handler for a different endpoint.
//
// Retry loop (task 104-028 showcase — the richest concurrency example in this codebase; read
// IPrincipalStore's Design region first, this handler is the "conflict policy stays with callers"
// half of that contract in action):
//   for up to MaxAttempts:
//     Get* (snapshot-on-get hands a fresh, caller-owned Credential — IPrincipalStore's Design region)
//     ownership/state checks (404 / 409 AlreadyRevoked / 409 LastCredential)
//     Revoke() the in-hand snapshot, then Update*
//     Update* throws ConcurrencyConflictException when a concurrent writer advanced the STORED
//       Version between this loop's Get and Update — the in-hand snapshot is now stale; the store
//       guarantees its own state is untouched on this throw (IPrincipalStore's Design region), so
//       there is nothing to roll back — just re-Get and retry with fresh state.
//     A lost race resolves itself naturally on retry: if the concurrent writer revoked THIS SAME
//       credential, the next loop iteration's Get returns IsRevoked=true and the loop returns 409
//       AlreadyRevoked instead of retrying again — no special-case code needed for "someone else beat
//       me to revoking this exact row."
//   MaxAttempts=3, then 409 TooMuchContention (a distinct, retryable-by-the-CALLER signal — contention
//     this sustained on a single credential row, revoked by its own owner, is not expected in
//     practice; three attempts is enough to absorb an incidental collision without masking a
//     pathological hot-loop as silent success).
//
// Already-revoked -> 409, NOT idempotent 204 (a documented choice, not the only defensible one):
// Credential.Revoke() models revoke as one-shot (throws InvalidOperationException on a second call) —
// returning 409 here keeps the handler's behavior honest about that domain rule (the caller's
// intended action did not happen because it already happened) rather than silently swallowing a
// stale-state call into a success code. It also means the retry loop needs the IsRevoked branch
// regardless (see above), so treating "already revoked" as a genuine 409 costs nothing extra to
// implement. Alternative considered: idempotent 204 (repeated revoke calls of an already-revoked
// credential "succeed" trivially) is a legitimate, common REST convention and would also be
// defensible — not chosen here so the response space stays small and uniformly signals "the state you
// expected does not hold" via 409 across every rejection branch (last-credential, already-revoked,
// contention) in this handler. Revisit if a real client workflow wants revoke-is-idempotent semantics.
//
// Cannot revoke the last ACTIVE credential -> 409 (task 104-005 requirement: prevent self-lockout;
// account recovery is explicitly out of scope — see the task's scope boundaries). Counted via
// ListCredentialsAsync(callerId, includeRevoked:false) INSIDE the retry loop (re-checked every
// attempt, not just once up front) so a credential that was the second-to-last active one at loop
// start but became the LAST active one due to a concurrent revoke of a sibling credential is caught
// on retry, not just at entry.
//
// Multi-revoke count TOCTOU — accepted Wave-1 residual, NOT closed by this handler (documented, not
// silently missed): the last-credential guard above protects the COMMON single-actor case and the
// case where the SAME credential is contended. It does NOT serialize two concurrent revokes of TWO
// DIFFERENT credentials belonging to the same principal: if a principal has exactly 2 active
// credentials and two concurrent RevokeCredential calls target the two DIFFERENT ids, each call's
// Get/count sees 2 active (>1, guard passes) and revokes its OWN target row — neither Update* call
// conflicts with the other (they touch different Credential rows, so their Version checks are
// independent), so both succeed and the principal legitimately reaches ZERO active credentials
// despite the guard. This is a real TOCTOU on the aggregate COUNT, not a bug in the per-row version
// check — the version token protects a single row, not the principal-wide invariant "at least one
// active credential." True fixes (deferred, Wave-2+): a principal-level version/lock the revoke path
// also takes (serializes ALL concurrent revokes for one principal, not just same-row ones), or a
// store-level atomic "revoke unless this would be the last active one" operation that evaluates the
// count and the write together under one lock (IPrincipalStore has no such method today — adding one
// would be a port surface change, out of this task's scope). Wave-1 accepts this residual because the
// realistic trigger (a principal deliberately double-clicking "revoke" on two different credentials
// within the same request-processing window) is a narrow, low-consequence race — the principal ends
// up in the SAME lockout state a sequential "revoke both" would also produce, just without the guard
// catching the second one first. Recorded here per the repo's documented-race convention (see
// AddCredentialAsync's own residual-orphan-Principal note on CompletePasskeyRegistration.Handler for
// the precedent of documenting rather than silently accepting or over-engineering a fix).
#endregion

#region Open Questions
// Should the last-credential guard become a store-level atomic check (closing the multi-revoke count
// TOCTOU above) once a second caller (e.g. an admin-initiated bulk revoke) needs the stronger
// guarantee? No such caller exists yet — revisit if/when one is proposed rather than speculatively
// widening IPrincipalStore's surface now.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.RevokeCredential;

public sealed partial class RevokeCredential
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private const int MaxAttempts = 3;

    private readonly IPrincipalStore PrincipalStore;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;

    public Handler(IPrincipalStore principalStore, ICurrentPrincipalAccessor currentPrincipalAccessor)
    {
      PrincipalStore = principalStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken);
      if (callerId is null)
      {
        return IdentityProblems.Unauthenticated();
      }

      var credentialId = CredentialId.From(command.CredentialId);

      for (int attempt = 0; attempt < MaxAttempts; attempt++)
      {
        Credential? credential = await PrincipalStore.GetCredentialAsync(credentialId, cancellationToken);
        if (credential is null || credential.PrincipalId != callerId.Value)
        {
          // Unknown id and "belongs to someone else" are indistinguishable on the wire — no
          // existence oracle (see Design region).
          return IdentityProblems.NotFound();
        }

        if (credential.IsRevoked)
        {
          return IdentityProblems.AlreadyRevoked();
        }

        IReadOnlyList<Credential> active =
          await PrincipalStore.ListCredentialsAsync(callerId.Value, includeRevoked: false, cancellationToken);
        if (active.Count <= 1)
        {
          return IdentityProblems.LastCredential();
        }

        credential.Revoke();

        try
        {
          await PrincipalStore.UpdateCredentialAsync(credential, cancellationToken);
          return new Response();
        }
        catch (ConcurrencyConflictException)
        {
          // Stale snapshot — a concurrent writer advanced the stored Version since this iteration's
          // Get. Store state is untouched by the failed Update*; re-Get and retry (Design region).
        }
      }

      return IdentityProblems.TooMuchContention();
    }
  }
}
