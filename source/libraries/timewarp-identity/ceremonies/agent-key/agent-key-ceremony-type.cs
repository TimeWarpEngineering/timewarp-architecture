#region Purpose
// Discriminates which agent-key ceremony a challenge was issued for, so a challenge minted for one
// ceremony can never be consumed by the other — the agent-key analog of WebAuthnCeremonyType.
#endregion

#region Design
// Reserved zero (None) so a default/uninitialized value fails closed rather than matching either
// real ceremony — mirrors WebAuthnCeremonyType/PrincipalKind/CredentialType/TrustTier's reserved-zero
// convention. A separate enum from WebAuthnCeremonyType (not shared) because the two ceremony
// families are domain-separated by design (AgentKeyProof's signed-data prefixes are distinct from
// WebAuthn's clientDataJSON "type" field) — sharing one enum would blur that separation and let a
// generic-store misuse route an agent-key challenge into the WebAuthn store's key space or vice versa.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Ceremony a one-time agent-key challenge was issued for — registration and token issuance stay domain-separated.
/// </summary>
public enum AgentKeyCeremonyType
{
  /// <summary>Uninitialized value; fails closed and matches no real ceremony.</summary>
  None = 0,

  /// <summary>Register a new agent public key against a principal.</summary>
  Registration = 1,

  /// <summary>Prove possession of an existing agent key to mint an access token.</summary>
  TokenIssuance = 2,
}
