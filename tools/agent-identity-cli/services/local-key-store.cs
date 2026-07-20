#region Purpose
// Sidecar JSON next to the PEM key file: principalId, keyId, last access token.
#endregion
#region Design
// Store path is derived from the key file (default.pem → default.store.json) so a
// single --key-file argument finds both materials. Never writes private key material
// into the store — only public ceremony outcomes (ids + bearer). File mode is
// owner-only where the platform allows (Unix 0600).
#endregion

namespace AgentIdentityCli.Services;

internal sealed class LocalKeyStore
{
  private readonly CliJson Json;

  public LocalKeyStore(CliJson json)
  {
    Json = json;
  }

  public AgentStoreRecord? TryLoad(string keyFilePath)
  {
    string storePath = PathDefaults.ResolveStorePath(keyFilePath);
    if (!File.Exists(storePath))
    {
      return null;
    }

    string text = File.ReadAllText(storePath);
    return Json.Deserialize<AgentStoreRecord>(text);
  }

  public void Save(string keyFilePath, AgentStoreRecord record)
  {
    ArgumentNullException.ThrowIfNull(record);
    string storePath = PathDefaults.ResolveStorePath(keyFilePath);
    string? directory = Path.GetDirectoryName(storePath);
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    string payload = Json.Serialize(record);
    File.WriteAllText(storePath, payload);
    TryRestrictPermissions(storePath);
  }

  public void UpdateRegistration(string keyFilePath, string principalId, string keyId)
  {
    AgentStoreRecord record = TryLoad(keyFilePath) ?? new AgentStoreRecord();
    record.PrincipalId = principalId;
    record.KeyId = keyId;
    Save(keyFilePath, record);
  }

  public void UpdateToken(string keyFilePath, string accessToken, string tokenType, int expiresInSeconds, IReadOnlyList<string> scopes, string? principalId = null)
  {
    AgentStoreRecord record = TryLoad(keyFilePath) ?? new AgentStoreRecord();
    record.AccessToken = accessToken;
    record.TokenType = tokenType;
    record.ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
    record.Scopes = [.. scopes];
    if (!string.IsNullOrEmpty(principalId))
    {
      record.PrincipalId = principalId;
    }

    Save(keyFilePath, record);
  }

  private static void TryRestrictPermissions(string path)
  {
    if (!OperatingSystem.IsWindows())
    {
      try
      {
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
      }
      catch (Exception ex)
      {
        _ = ex; // Best-effort: non-Unix or platform without chmod support.
      }
    }
  }
}

internal sealed class AgentStoreRecord
{
  public string? PrincipalId { get; set; }
  public string? KeyId { get; set; }
  public string? AccessToken { get; set; }
  public string? TokenType { get; set; }
  public DateTimeOffset? ExpiresAtUtc { get; set; }
  public List<string> Scopes { get; set; } = [];
}
