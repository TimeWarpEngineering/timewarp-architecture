#region Purpose
// Port for one-time agent-key challenge issuance/consumption — the agent-key analog of
// IWebAuthnChallengeStore, hosts supply the implementation (in-memory here).
#endregion

#region Design
// Same one-time/uniform-false-on-failure contract as IWebAuthnChallengeStore (see that port's
// Design region for the full rationale — not re-derived here). A SEPARATE port/type from
// IWebAuthnChallengeStore&lt;WebAuthnCeremonyType&gt; (not a shared generic interface) even though
// InMemoryAgentKeyChallengeStore's IMPLEMENTATION shares a core with InMemoryWebAuthnChallengeStore:
// the two ceremony families are domain-separated by design (see AgentKeyCeremonyType's Design
// region), and DI should bind IAgentKeyChallengeStore/IWebAuthnChallengeStore as two distinct
// singletons a caller cannot accidentally cross-wire.
#endregion

namespace TimeWarp.Identity;

public interface IAgentKeyChallengeStore
{
  /// <summary>Mints a new 32-byte random challenge recorded for the given ceremony type.</summary>
  byte[] Issue(AgentKeyCeremonyType ceremonyType);

  /// <summary>
  /// Attempts to consume (remove) a previously issued challenge for the given ceremony type.
  /// Returns false for a challenge that was never issued, already consumed, expired, or issued for
  /// a different ceremony type — callers must treat all of these identically.
  /// </summary>
  bool TryConsume(AgentKeyCeremonyType ceremonyType, byte[] challenge);
}
