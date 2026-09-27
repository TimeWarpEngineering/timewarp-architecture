#region Purpose
// Short, stable, non-reversible per-ACCOUNT discriminator derived from a PrincipalId — the same value on every passkey of one principal.
#endregion

#region Design
// Task 253: the WebAuthn user.name/displayName an authenticator stores is immutable after
// creation, so every passkey must carry an account-distinguishing name from the start. That name
// cannot use the per-credential CredentialFingerprint (248-001): it is derived from the credential
// id the authenticator only produces DURING the ceremony, and it differs per passkey. This value is
// per principal instead — SHA-256 over the id's 16 Guid bytes (Guid.TryWriteBytes, the standard
// little-endian layout), last 8 lowercase hex chars, the same shape as CredentialFingerprint so the
// two read alike in the UI. Display-safe: one-way and 32 bits, so it never reveals the raw id.
// Computed on demand — no column to drift; a principal's fingerprint can never change.
#endregion

namespace TimeWarp.Identity;

using System.Security.Cryptography;

/// <summary>
/// Derives the 8-hex-char display fingerprint for a principal (account).
/// </summary>
public static class PrincipalFingerprint
{
  /// <summary>Length of the fingerprint string in characters.</summary>
  public const int Length = 8;

  /// <summary>Last <see cref="Length"/> lowercase hex characters of SHA-256(principal id bytes).</summary>
  public static string Compute(PrincipalId principalId)
  {
    if (principalId.IsEmpty)
    {
      throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
    }

    Span<byte> idBytes = stackalloc byte[16];
    principalId.Value.TryWriteBytes(idBytes);

    Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
    SHA256.HashData(idBytes, digest);
    return Convert.ToHexStringLower(digest[^(Length / 2)..]);
  }
}
