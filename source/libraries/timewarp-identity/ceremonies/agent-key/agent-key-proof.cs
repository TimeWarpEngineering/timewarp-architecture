#region Purpose
// Builds the domain-separated signed-data blob for an agent-key ceremony and verifies an agent's
// signature over it — the browser-less analog of WebAuthn's clientDataJSON+signature binding.
#endregion

#region Design
// Proof of possession (task 104-004 §1): the agent signs UTF8(prefix) ‖ challenge, where prefix is
// ceremony-typed and DOMAIN-SEPARATED:
//   "TimeWarp.Identity.AgentKey.Register.v1:"
//   "TimeWarp.Identity.AgentKey.Token.v1:"
// Domain separation + a ceremony-typed one-time challenge (IAgentKeyChallengeStore) together close
// cross-endpoint replay: a signature produced for registration cannot verify for token issuance
// (different prefix → different signed bytes → signature does not match) even if an attacker somehow
// replayed the same challenge value across both stores, and separately the challenge itself cannot
// be reused even within one ceremony type (TryConsume is one-time). BuildSignedData is PUBLIC
// deliberately — fixtures/tests and eventual agent SDKs need to construct EXACTLY these bytes to
// sign, and a private/duplicated construction would risk silently drifting from what Verify checks.
// Signature format is DER (Rfc3279DerSequence) ONLY, matching 104-003's WebAuthn assertion path —
// P1363 (raw r‖s) is deliberately not accepted: supporting both formats for the same curve is a
// documented malleability/confusion surface for no Wave-1 benefit (single format, one way to verify).
// Verify's check order: TryImport (malformed/wrong-algorithm — never throws, see agent-public-key.cs)
// BEFORE the signature check — a key that doesn't parse or isn't P-256 fails fast without ever
// reaching VerifyData. Empirically verified (see agent-public-key.cs's Design region) that
// ECDsa.VerifyData never throws for a malformed/empty signature on this platform — it returns false
// — so no additional guard is needed around that call specifically, but the ecdsa import itself is
// still wrapped in try/finally-dispose (CA2000; mirrors cose-key.cs's TryCreateVerifier shape).
#endregion

namespace TimeWarp.Identity;

public static class AgentKeyProof
{
  private const string RegistrationPrefix = "TimeWarp.Identity.AgentKey.Register.v1:";
  private const string TokenIssuancePrefix = "TimeWarp.Identity.AgentKey.Token.v1:";

  /// <summary>Builds UTF8(prefix) ‖ challenge for the given ceremony type — the exact bytes an agent must sign.</summary>
  public static byte[] BuildSignedData(AgentKeyCeremonyType ceremonyType, byte[] challenge)
  {
    ArgumentNullException.ThrowIfNull(challenge);

    byte[] prefixBytes = Encoding.UTF8.GetBytes(GetPrefix(ceremonyType));
    return [.. prefixBytes, .. challenge];
  }

  public static AgentKeyProofResult Verify(AgentKeyCeremonyType ceremonyType, byte[] publicKeySpki, byte[] challenge, byte[] signature)
  {
    ArgumentNullException.ThrowIfNull(publicKeySpki);
    ArgumentNullException.ThrowIfNull(challenge);
    ArgumentNullException.ThrowIfNull(signature);

    // Explicit try/finally (not a using declaration): CA2000 cannot prove `ecdsa` is disposed on
    // TryImport's false path when the out-param call happens before the try region — the canonical
    // "local declared before try, disposed unconditionally in finally" shape (same as
    // cose-key.cs's TryCreateVerifier callers in 104-003) is what satisfies the analyzer here.
    ECDsa? ecdsa = null;
    try
    {
      if (!AgentPublicKey.TryImport(publicKeySpki, out ecdsa, out bool isP256))
      {
        return AgentKeyProofResult.Failure(AgentKeyFailureReason.MalformedPublicKey);
      }

      if (!isP256)
      {
        return AgentKeyProofResult.Failure(AgentKeyFailureReason.UnsupportedAlgorithm);
      }

      byte[] signedData = BuildSignedData(ceremonyType, challenge);
      bool verified = ecdsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

      return verified
        ? AgentKeyProofResult.Success()
        : AgentKeyProofResult.Failure(AgentKeyFailureReason.SignatureInvalid);
    }
    finally
    {
      ecdsa?.Dispose();
    }
  }

  private static string GetPrefix(AgentKeyCeremonyType ceremonyType) => ceremonyType switch
  {
    AgentKeyCeremonyType.Registration => RegistrationPrefix,
    AgentKeyCeremonyType.TokenIssuance => TokenIssuancePrefix,
    _ => throw new ArgumentOutOfRangeException(nameof(ceremonyType), ceremonyType, "AgentKeyCeremonyType must be Registration or TokenIssuance.")
  };
}
