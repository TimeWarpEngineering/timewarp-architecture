#region Purpose
// Port for one-time WebAuthn challenge issuance/consumption — hosts supply the implementation
// (in-memory here; a distributed store is out of scope for this task).
#endregion

#region Design
// Issue returns the raw 32-byte challenge (not an id) — the same bytes travel to the browser inside
// the options JSON and come back embedded in clientDataJSON's "challenge" field; the challenge
// value itself IS the lookup key, so no separate correlation id exists to leak or guess around.
// TryConsume is one-time by construction: a successful consume removes the entry, so a replayed
// clientDataJSON can never verify twice. It returns false uniformly for unknown, expired, or
// wrong-ceremony-type challenges — callers (the complete-* handlers) must not distinguish these
// cases in their response (a distinguishable "expired" vs "wrong type" vs "never issued" response
// would leak ceremony-timing information to an attacker probing the endpoint).
// Pending principal id (task 253): a registration start may pre-allocate the PrincipalId its
// completion will mint (so the WebAuthn user name can carry that account's fingerprint) and keep
// it WITH the challenge via Issue(type, pendingPrincipalId). It never travels on the wire — the
// completing handler reads it back from the same one-time TryConsume, so a client cannot supply or
// swap it, and an abandoned/expired challenge allocates nothing persistent.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// One-time WebAuthn challenge issuance and consumption — fail closed with a uniform false on every miss.
/// </summary>
public interface IWebAuthnChallengeStore
{
  /// <summary>Mints a new 32-byte random challenge recorded for the given ceremony type.</summary>
  byte[] Issue(WebAuthnCeremonyType ceremonyType);

  /// <summary>
  /// Mints a new 32-byte random challenge recorded for the given ceremony type, keeping
  /// <paramref name="pendingPrincipalId"/> server-side with it until it is consumed or expires.
  /// </summary>
  byte[] Issue(WebAuthnCeremonyType ceremonyType, PrincipalId? pendingPrincipalId);

  /// <summary>
  /// Attempts to consume (remove) a previously issued challenge for the given ceremony type.
  /// Returns false for a challenge that was never issued, already consumed, expired, or issued for
  /// a different ceremony type — callers must treat all of these identically.
  /// </summary>
  bool TryConsume(WebAuthnCeremonyType ceremonyType, byte[] challenge);

  /// <summary>
  /// Same one-time consume as <see cref="TryConsume(WebAuthnCeremonyType, byte[])"/>, also returning
  /// the pending principal id recorded at issue (null when none was recorded or the consume failed).
  /// </summary>
  bool TryConsume(WebAuthnCeremonyType ceremonyType, byte[] challenge, out PrincipalId? pendingPrincipalId);
}
