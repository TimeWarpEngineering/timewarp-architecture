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

public enum AgentKeyFailureReason
{
  None = 0,
  MalformedPublicKey = 1,
  UnsupportedAlgorithm = 2,
  SignatureInvalid = 3,
}
