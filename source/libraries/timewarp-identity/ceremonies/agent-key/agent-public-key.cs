#region Purpose
// Parses/validates an agent's ECDSA P-256 public key, wire-encoded as base64url-decoded SPKI DER
// (RFC 5280 SubjectPublicKeyInfo), and derives its server-computed key id.
#endregion

#region Design
// Wire format decision (task 104-004): ECDSA P-256 (ES256) only, wire = SPKI DER — not COSE (agent
// SDKs speak openssl/Python/Node/Go/WebCrypto, none of which emit CBOR natively; SPKI DER is each
// of those runtimes' native public-key export format) and not Ed25519 (not in the .NET 10 BCL —
// verified against Microsoft.NETCore.App.Ref 10.0.10, only composite ML-DSA identifiers exist;
// supporting it would need NSec/BouncyCastle, forbidden by the same posture that rejected
// Fido2NetLib in 104-003). Revisit trigger: if Ed25519 lands in a future BCL, or a consumer
// genuinely needs it, add it as a second accepted algorithm here — the public TryParse/keyId
// contract does not change, only which curves/algorithms IsP256-equivalent-checks accept.
// KeyId = SHA-256(spkiDer), server-computed (never client-supplied) — the SAME canonical bytes that
// were validated (trailing-byte-free, exact curve) are what gets hashed, so two different byte
// strings that both "represent" the same key (e.g. one with trailing garbage) cannot hash to
// different ids only for one of them to later fail Verify — TryParse's rejection of trailing bytes
// makes the hash-domain and the accept-domain identical.
// Guards run BEFORE any BCL import call, not caught afterward (round-1/round-2 lessons from
// 104-003's cose-key.cs, specifically finding M9: an empty byte[] passed into RSA import threw
// IndexOutOfRangeException on this platform, NOT CryptographicException, escaping a
// catch-CryptographicException-only guard). Empirically verified for THIS API on this platform
// before writing this guard (not assumed): ECDsa.ImportSubjectPublicKeyInfo throws
// CryptographicException — never IndexOutOfRangeException or another type — for empty input,
// garbage bytes, truncated DER, and a non-EC (RSA) SPKI; the explicit `Length == 0` check here is
// still kept as defense-in-depth (never rely on a single BCL call's current behavior being the only
// thing standing between adversarial input and an uncaught exception), and doubles as a fast-reject
// for the common "no key sent" case without an exception at all.
// Trailing-DER rejection: ImportSubjectPublicKeyInfo's `out bytesRead` reports exactly how many
// bytes the ASN.1 structure consumed; `bytesRead != spkiDer.Length` means extra bytes were appended
// after a structurally valid SPKI — rejected so no ambiguity exists about "the" bytes this key's
// id is computed from (see KeyId rationale above). Confirmed empirically: an otherwise-valid P-256
// SPKI with 4 trailing bytes imports successfully with bytesRead short of the input length.
// TryImport is the shared internal seam between this file's public TryParse and
// AgentKeyProof.Verify: it does the ONE ImportSubjectPublicKeyInfo call (avoids double-parsing the
// same bytes) and reports both "did this parse as SOME EC key at all" and "is it P-256 specifically"
// — TryParse folds both into a single accept/reject bool (its only public contract), while Verify
// uses the finer split to produce AgentKeyFailureReason.MalformedPublicKey (did not parse as EC at
// all — covers RSA, which ImportSubjectPublicKeyInfo rejects with CryptographicException, confirmed
// empirically) versus UnsupportedAlgorithm (parsed as a real EC key, just not the P-256 curve this
// verifier accepts — e.g. P-384).
#endregion

namespace TimeWarp.Identity;

public static class AgentPublicKey
{
  private const int MaxSpkiLength = 2 * 1024;

  /// <summary>
  /// Validates that <paramref name="spkiDer"/> is a well-formed ECDSA P-256 SubjectPublicKeyInfo
  /// (no trailing bytes, curve OID must be P-256) and derives its key id (SHA-256 of the exact
  /// input bytes). Never throws on adversarial input.
  /// </summary>
  public static bool TryParse(byte[] spkiDer, out byte[] keyId)
  {
    keyId = [];

    // Explicit try/finally (not a using declaration): CA2000 cannot prove `ecdsa` is disposed on
    // TryImport's false path when the out-param call happens before the try region — the canonical
    // "local declared before try, disposed unconditionally in finally" shape (same as
    // cose-key.cs's TryCreateVerifier callers in 104-003) is what satisfies the analyzer here.
    ECDsa? ecdsa = null;
    try
    {
      if (!TryImport(spkiDer, out ecdsa, out bool isP256))
      {
        return false;
      }

      if (!isP256)
      {
        return false;
      }

      keyId = SHA256.HashData(spkiDer);
      return true;
    }
    finally
    {
      ecdsa?.Dispose();
    }
  }

  /// <summary>
  /// Imports <paramref name="spkiDer"/> as an EC public key with no curve restriction, reporting
  /// separately whether it parsed at all and whether the curve is P-256. Internal — shared only with
  /// AgentKeyProof.Verify, which needs the finer-grained outcome TryParse's single bool folds away.
  /// Caller owns disposing the returned ECDsa when non-null.
  /// </summary>
  internal static bool TryImport(byte[] spkiDer, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ECDsa? ecdsa, out bool isP256)
  {
    ecdsa = null;
    isP256 = false;

    if (spkiDer is null || spkiDer.Length == 0 || spkiDer.Length > MaxSpkiLength)
    {
      return false;
    }

    ECDsa? candidate = null;
    try
    {
      candidate = ECDsa.Create();
      candidate.ImportSubjectPublicKeyInfo(spkiDer, out int bytesRead);

      if (bytesRead != spkiDer.Length)
      {
        candidate.Dispose();
        return false;
      }

      ECParameters parameters = candidate.ExportParameters(includePrivateParameters: false);
      isP256 = string.Equals(parameters.Curve.Oid.Value, ECCurve.NamedCurves.nistP256.Oid.Value, StringComparison.Ordinal);
      ecdsa = candidate;
      return true;
    }
    catch (CryptographicException)
    {
      candidate?.Dispose();
      ecdsa = null;
      return false;
    }
  }
}
