#region Purpose
// Parses a COSE_Key CBOR map (RFC 8152 §7/§13) into the EC2 or RSA fields this verifier supports,
// and builds the corresponding BCL asymmetric algorithm for signature verification.
#endregion

#region Design
// COSE_Key members are integer-keyed, and label -1/-2/-3 mean DIFFERENT things depending on kty
// (EC2: crv/x/y; RSA: n/e/unused) — so member order cannot be assumed and label -1's CBOR TYPE
// (integer for EC2's crv, byte string for RSA's n) is used to disambiguate as each entry is read,
// then the fields are reinterpreted once kty is known. This tolerates any encoder's member order
// (canonical CBOR sorts by key, but this parser does not require it).
// Template posture bounds scope to ES256 (-7, EC2/P-256) and RS256 (-257, RSA) — TryCreateVerifier
// returns false for any other alg, and for a structurally-inconsistent key (e.g. kty=RSA but alg=
// ES256), rather than guessing. ECDsa.Create/RSA.ImportParameters validate the key material itself
// (bad curve point, wrong-length modulus, etc.) and their CryptographicException becomes a false
// return rather than propagating — every failure in this file is a Try* bool, never a throw, so
// WebAuthnRegistration/WebAuthnAuthentication.Verify stay exception-free on the adversarial path.
// RS256 additionally rejects moduli below MinimumRsaModulusBits (2048) — a round-1 security review
// noted RSA.ImportParameters happily imports a structurally-valid-but-weak (e.g. 512-bit) modulus
// with no minimum-strength check. Registration only mints a NEW Principal for the presenter, so a
// weak key is self-inflicted (defense-in-depth, not an exploitable gap), but a trust kernel
// shouldn't default to accepting sub-2048-bit RSA keys. The check happens in TryCreateVerifier (not
// TryParse) so a weak key still maps to WebAuthnFailureReason.UnsupportedAlgorithm — the same
// reason an unsupported alg/curve produces — rather than a new reason, since "this key is
// structurally parseable but this verifier will not use it" is the same failure class either way.
#endregion

namespace TimeWarp.Identity;

internal sealed class CoseKey
{
  private const int LabelKeyType = 1;
  private const int LabelAlgorithm = 3;
  private const int LabelCrvOrN = -1;
  private const int LabelXOrE = -2;
  private const int LabelY = -3;

  private const int KeyTypeEc2 = 2;
  private const int KeyTypeRsa = 3;
  private const int CurveP256 = 1;

  private const int AlgorithmEs256 = -7;
  private const int AlgorithmRs256 = -257;

  private const int MinimumRsaModulusBits = 2048;

  public required int KeyType { get; init; }
  public required int Algorithm { get; init; }
  public byte[]? X { get; init; }
  public byte[]? Y { get; init; }
  public byte[]? Modulus { get; init; }
  public byte[]? Exponent { get; init; }

  public static bool TryParse(CborReader reader, out CoseKey? key)
  {
    key = null;

    try
    {
      reader.ReadStartMap();

      int? keyType = null;
      int? algorithm = null;
      int? curve = null;
      byte[]? crvOrNBytes = null;
      byte[]? xOrEBytes = null;
      byte[]? yBytes = null;

      while (reader.PeekState() != CborReaderState.EndMap)
      {
        int label = reader.ReadInt32();
        switch (label)
        {
          case LabelKeyType:
            keyType = reader.ReadInt32();
            break;
          case LabelAlgorithm:
            algorithm = reader.ReadInt32();
            break;
          case LabelCrvOrN:
            if (reader.PeekState() is CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger)
            {
              curve = reader.ReadInt32();
            }
            else
            {
              crvOrNBytes = reader.ReadByteString();
            }

            break;
          case LabelXOrE:
            xOrEBytes = reader.ReadByteString();
            break;
          case LabelY:
            yBytes = reader.ReadByteString();
            break;
          default:
            reader.SkipValue();
            break;
        }
      }

      reader.ReadEndMap();

      if (keyType is null || algorithm is null) return false;

      if (keyType == KeyTypeEc2)
      {
        if (curve != CurveP256 || xOrEBytes is null || yBytes is null) return false;

        key = new CoseKey { KeyType = keyType.Value, Algorithm = algorithm.Value, X = xOrEBytes, Y = yBytes };
        return true;
      }

      if (keyType == KeyTypeRsa)
      {
        if (crvOrNBytes is null || xOrEBytes is null) return false;

        key = new CoseKey { KeyType = keyType.Value, Algorithm = algorithm.Value, Modulus = crvOrNBytes, Exponent = xOrEBytes };
        return true;
      }

      return false;
    }
    catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
    {
      key = null;
      return false;
    }
  }

  public bool TryCreateVerifier(out AsymmetricAlgorithm? algorithm, out HashAlgorithmName hashAlgorithm)
  {
    algorithm = null;
    hashAlgorithm = default;

    try
    {
      if (Algorithm == AlgorithmEs256 && KeyType == KeyTypeEc2 && X is not null && Y is not null)
      {
        ECParameters ecParameters = new()
        {
          Curve = ECCurve.NamedCurves.nistP256,
          Q = new ECPoint { X = X, Y = Y }
        };

        algorithm = ECDsa.Create(ecParameters);
        hashAlgorithm = HashAlgorithmName.SHA256;
        return true;
      }

      if (Algorithm == AlgorithmRs256 && KeyType == KeyTypeRsa && Modulus is not null && Exponent is not null)
      {
        if (GetModulusBitLength(Modulus) < MinimumRsaModulusBits) return false;

        var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters { Modulus = Modulus, Exponent = Exponent });
        algorithm = rsa;
        hashAlgorithm = HashAlgorithmName.SHA256;
        return true;
      }

      return false;
    }
    catch (CryptographicException)
    {
      algorithm = null;
      hashAlgorithm = default;
      return false;
    }
  }

  // Exact bit length, ignoring leading zero bytes/bits — COSE/DER-derived moduli are not guaranteed
  // to be minimal-length encodings, so `Modulus.Length * 8` alone could over-report by up to a byte.
  private static int GetModulusBitLength(byte[] modulus)
  {
    int index = 0;
    while (index < modulus.Length - 1 && modulus[index] == 0) index++;

    int bitLength = (modulus.Length - index) * 8;
    byte leading = modulus[index];
    while (leading != 0 && (leading & 0x80) == 0)
    {
      bitLength--;
      leading <<= 1;
    }

    return bitLength;
  }
}
