// ReSharper disable InconsistentNaming
namespace LocalKeyStore_;

public class Sidecar_RoundTrip
{
  public void Save_and_load_preserves_registration_and_token_fields()
  {
    string tempDir = Path.Combine(Path.GetTempPath(), $"agent-store-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);
    string keyFile = Path.Combine(tempDir, "default.pem");
    File.WriteAllText(keyFile, "placeholder");

    try
    {
      var store = new LocalKeyStore(new CliJson());
      store.UpdateRegistration(keyFile, principalId: "11111111-1111-1111-1111-111111111111", keyId: "abcKeyId");
      store.UpdateToken(
        keyFile,
        accessToken: "tok-value",
        tokenType: "Bearer",
        expiresInSeconds: 3600,
        scopes: ["identity:read"],
        principalId: "11111111-1111-1111-1111-111111111111");

      AgentStoreRecord? loaded = store.TryLoad(keyFile);
      loaded.ShouldNotBeNull();
      loaded.PrincipalId.ShouldBe("11111111-1111-1111-1111-111111111111");
      loaded.KeyId.ShouldBe("abcKeyId");
      loaded.AccessToken.ShouldBe("tok-value");
      loaded.TokenType.ShouldBe("Bearer");
      loaded.Scopes.ShouldBe(["identity:read"]);
      loaded.ExpiresAtUtc.ShouldNotBeNull();
      loaded.ExpiresAtUtc.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(30));

      PathDefaults.ResolveStorePath(keyFile).ShouldBe(Path.Combine(tempDir, "default.store.json"));
      File.Exists(PathDefaults.ResolveStorePath(keyFile)).ShouldBeTrue();
    }
    finally
    {
      try
      {
        Directory.Delete(tempDir, recursive: true);
      }
      catch (IOException)
      {
        // temp cleanup best-effort
      }
    }
  }

  public void TryLoad_returns_null_when_missing()
  {
    string keyFile = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.pem");
    var store = new LocalKeyStore(new CliJson());
    store.TryLoad(keyFile).ShouldBeNull();
  }
}
