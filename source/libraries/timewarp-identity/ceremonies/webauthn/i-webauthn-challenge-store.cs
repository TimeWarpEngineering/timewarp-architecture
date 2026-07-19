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
#endregion

namespace TimeWarp.Identity;

public interface IWebAuthnChallengeStore
{
  /// <summary>Mints a new 32-byte random challenge recorded for the given ceremony type.</summary>
  byte[] Issue(WebAuthnCeremonyType ceremonyType);

  /// <summary>
  /// Attempts to consume (remove) a previously issued challenge for the given ceremony type.
  /// Returns false for a challenge that was never issued, already consumed, expired, or issued for
  /// a different ceremony type — callers must treat all of these identically.
  /// </summary>
  bool TryConsume(WebAuthnCeremonyType ceremonyType, byte[] challenge);
}
