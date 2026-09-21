#region Purpose
// Enumerates every reason AgentKeyProof.Verify or AgentPublicKey.TryParse can reject an agent-key
// ceremony, so callers can branch on cause without parsing exception messages.
#endregion

#region Design
// Reserved zero (None) pairs with AgentKeyProofResult.IsValid true — a valid result always carries
// FailureReason.None, mirroring WebAuthnFailureReason's convention.
// Three reasons only (not WebAuthn's thirteen): agent-key proof has no attestation object, no
// authenticator-data flags, no rpIdHash/origin binding — proof of possession here is exactly
// "does this SPKI-encoded P-256 public key structurally parse (and is it P-256), and does the
// signature verify against the domain-separated challenge." MalformedPublicKey covers both
// AgentPublicKey.TryParse failures (empty/oversize/trailing-bytes/unparseable) AND
// AgentKeyProof.Verify's own re-parse failing for the same reasons — the two functions share this
// reason rather than needing a distinct "verify-time parse failure" value, since from a caller's
// perspective both mean "this key material is not usable."
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Why <see cref="AgentKeyProof.Verify"/> or <see cref="AgentPublicKey.TryParse"/> rejected agent-key material.
/// </summary>
public enum AgentKeyFailureReason
{
  /// <summary>No failure; pairs with a valid proof result.</summary>
  None = 0,

  /// <summary>Public key bytes are empty, oversized, trailing-padded, or not a parseable EC SPKI.</summary>
  MalformedPublicKey = 1,

  /// <summary>Key parsed as EC but is not the accepted P-256 curve.</summary>
  UnsupportedAlgorithm = 2,

  /// <summary>ECDSA signature does not verify over the domain-separated challenge bytes.</summary>
  SignatureInvalid = 3,
}
