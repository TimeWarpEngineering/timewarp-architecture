#region Purpose
// Outcome of AgentKeyProof.Verify: whether the signature proved possession of the claimed key, or
// why it did not.
#endregion

#region Design
// Private constructor + internal factory methods (mirrors WebAuthnRegistrationResult/
// WebAuthnAssertionResult) — a caller can never construct a "valid" result with a non-None
// FailureReason or vice versa. No payload byte[] fields (unlike WebAuthnRegistrationResult): proof
// verification produces no new material to hand back — the caller already has the public key and
// (for registration) derives KeyId itself via AgentPublicKey.TryParse.
#endregion

namespace TimeWarp.Identity;

public sealed class AgentKeyProofResult
{
  private AgentKeyProofResult(bool isValid, AgentKeyFailureReason failureReason)
  {
    IsValid = isValid;
    FailureReason = failureReason;
  }

  public bool IsValid { get; }

  public AgentKeyFailureReason FailureReason { get; }

  internal static AgentKeyProofResult Success() => new(true, AgentKeyFailureReason.None);

  internal static AgentKeyProofResult Failure(AgentKeyFailureReason reason) => new(false, reason);
}
