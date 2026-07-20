#region Purpose
// P-256 keygen, PEM load/store, SPKI export, and domain-separated ceremony signing.
#endregion
#region Design
// MANDATORY library pin (task 104-029):
//   byte[] signedData = AgentKeyProof.BuildSignedData(ceremonyType, challengeBytes);
//   byte[] signature = ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
// Do NOT reimplement Verify here — call AgentKeyProof.Verify from tests only.
// Wire encoding is System.Buffers.Text.Base64Url for challenge/publicKey/signature/keyId.
// Private key PEM is PKCS#8; never logged.
#endregion

namespace AgentIdentityCli.Services;

internal sealed class AgentSigning
{
  public GeneratedKey GenerateKey()
  {
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    byte[] spki = ecdsa.ExportSubjectPublicKeyInfo();
    if (!AgentPublicKey.TryParse(spki, out byte[] keyId))
    {
      throw new InvalidOperationException("Generated P-256 SPKI failed AgentPublicKey.TryParse — library contract broken.");
    }

    byte[] pkcs8 = ecdsa.ExportPkcs8PrivateKey();
    string pem = PemEncoding.WriteString("PRIVATE KEY", pkcs8);
    return new GeneratedKey(pem, spki, keyId);
  }

  public void WriteKeyFile(string path, string pem, bool force)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    ArgumentException.ThrowIfNullOrWhiteSpace(pem);

    if (File.Exists(path) && !force)
    {
      throw new InvalidOperationException($"Key file already exists: {path}. Pass --force to overwrite.");
    }

    string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    File.WriteAllText(path, pem);
    if (!OperatingSystem.IsWindows())
    {
      try
      {
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
      }
      catch (Exception ex)
      {
        _ = ex; // Best-effort permissions.
      }
    }
  }

  public LoadedKey LoadKey(string path)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    if (!File.Exists(path))
    {
      throw new FileNotFoundException($"Key file not found: {path}", path);
    }

    string pem = File.ReadAllText(path);
    using var ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(pem);

    byte[] spki = ecdsa.ExportSubjectPublicKeyInfo();
    if (!AgentPublicKey.TryParse(spki, out byte[] keyId))
    {
      throw new InvalidOperationException("Key file SPKI is not a valid agent P-256 public key (AgentPublicKey.TryParse failed).");
    }

    // Re-import into a caller-owned instance (the using above disposes).
    var owned = ECDsa.Create();
    owned.ImportFromPem(pem);
    return new LoadedKey(owned, spki, keyId);
  }

  public byte[] Sign(ECDsa ecdsa, AgentKeyCeremonyType ceremonyType, byte[] challengeBytes)
  {
    ArgumentNullException.ThrowIfNull(ecdsa);
    ArgumentNullException.ThrowIfNull(challengeBytes);

    // Library pin — exact construction Verify checks.
    byte[] signedData = AgentKeyProof.BuildSignedData(ceremonyType, challengeBytes);
    return ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
  }

  public static string ToBase64Url(byte[] data) => Base64Url.EncodeToString(data);

  public static byte[] FromBase64Url(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    int maxLength = Base64Url.GetMaxDecodedLength(value.Length);
    byte[] buffer = new byte[maxLength];
    if (!Base64Url.TryDecodeFromChars(value.AsSpan(), buffer, out int bytesWritten))
    {
      throw new FormatException("Value is not valid base64url.");
    }

    if (bytesWritten == buffer.Length)
    {
      return buffer;
    }

    return buffer.AsSpan(0, bytesWritten).ToArray();
  }
}

internal sealed record GeneratedKey(string Pem, byte[] SpkiPublicKey, byte[] KeyId);

internal sealed class LoadedKey : IDisposable
{
  public LoadedKey(ECDsa ecdsa, byte[] spkiPublicKey, byte[] keyId)
  {
    Ecdsa = ecdsa;
    SpkiPublicKey = spkiPublicKey;
    KeyId = keyId;
  }

  public ECDsa Ecdsa { get; }
  public byte[] SpkiPublicKey { get; }
  public byte[] KeyId { get; }

  public void Dispose() => Ecdsa.Dispose();
}
