#region Purpose
// Short, stable, non-reversible discriminator for a credential derived from its handle bytes — safe to show in a list and to read back over the wire.
#endregion

#region Design
// Task 248-001: the UI needs something that tells two same-provider passkeys apart without ever
// shipping Credential.Handle (the WebAuthn credential id / agent key id — see Credential's Design
// region on why material never leaves the server). SHA-256 over the handle, last 8 lowercase hex
// chars (32 bits): stable across restarts and stores, one-way, and far too short to reconstruct the
// handle from. Collisions among one principal's handful of credentials are astronomically unlikely
// and harmless (the row's CredentialId remains the real key; the fingerprint is display-only).
// Computed on demand from the stored handle rather than persisted — no column to drift.
#endregion

namespace TimeWarp.Identity;

using System.Security.Cryptography;

/// <summary>
/// Derives the 8-hex-char display fingerprint for a credential handle.
/// </summary>
public static class CredentialFingerprint
{
  /// <summary>Length of the fingerprint string in characters.</summary>
  public const int Length = 8;

  /// <summary>Last <see cref="Length"/> lowercase hex characters of SHA-256(handle).</summary>
  public static string Compute(ReadOnlySpan<byte> handle)
  {
    if (handle.IsEmpty)
    {
      throw new ArgumentException("Handle must be non-empty.", nameof(handle));
    }

    Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
    SHA256.HashData(handle, digest);
    return Convert.ToHexStringLower(digest[^(Length / 2)..]);
  }
}
