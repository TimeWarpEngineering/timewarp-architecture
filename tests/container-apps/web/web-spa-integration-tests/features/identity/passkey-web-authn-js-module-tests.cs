#region Purpose
// Passkey JS interop uses on-demand import of web-authn.js named exports, not window.Spa.
#endregion

#region Design
// Task 200: Spa.WebAuthn.* string identifiers threw "'Spa' was undefined" when the host
// initializer list omitted Web.Spa. WebAuthnJsModule must call IJSRuntime identifier "import"
// with ./js/features/web-authn.js, then the named export. Host-free recording IJSRuntime —
// no HostGraph, no browser.
#endregion

namespace PasskeyWebAuthnJsModule_;

using Microsoft.JSInterop;
using TimeWarp.Architecture.Services;

[TestTag("Unit")]
public class Import_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Import_Given_>();

  public static async Task CreateCredential_Should_ImportModuleNotWindowSpa()
  {
    RecordingJsRuntime jsRuntime = new();

    string json = await WebAuthnJsModule.CreateCredentialAsync(
      jsRuntime,
      "{}",
      preferHybrid: false,
      CancellationToken.None);

    jsRuntime.Identifiers.ShouldBe(["import"]);
    jsRuntime.ImportedSpecifier.ShouldBe(WebAuthnJsModule.Specifier);
    jsRuntime.Module.ExportNames.ShouldBe(["CreateCredential"]);
    json.ShouldContain("credentialId");
  }

  public static async Task GetCredential_Should_ImportModuleNotWindowSpa()
  {
    RecordingJsRuntime jsRuntime = new();

    string json = await WebAuthnJsModule.GetCredentialAsync(
      jsRuntime,
      "{}",
      preferHybrid: true,
      CancellationToken.None);

    jsRuntime.Identifiers.ShouldBe(["import"]);
    jsRuntime.ImportedSpecifier.ShouldBe("./js/features/web-authn.js");
    jsRuntime.Module.ExportNames.ShouldBe(["GetCredential"]);
    json.ShouldContain("authenticatorData");
  }

  public static Task CallSites_Should_NotUseSpaWebAuthnStringIdentifiers()
  {
    string repoRoot = FindRepoRoot();
    string[] files =
    [
      Path.Combine(repoRoot, "source/container-apps/web/projects/web-spa/services/passkey-ceremony-client.cs"),
      Path.Combine(
        repoRoot,
        "source/container-apps/web/projects/web-spa/features/identity/credentials-state/credentials-state.add-passkey.cs")
    ];

    foreach (string file in files)
    {
      File.Exists(file).ShouldBeTrue(file);
      string source = File.ReadAllText(file);
      source.ShouldNotContain("Spa.WebAuthn");
    }

    return Task.CompletedTask;
  }

  private static string FindRepoRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }

  private sealed class RecordingJsRuntime : IJSRuntime
  {
    public List<string> Identifiers { get; } = [];
    public string? ImportedSpecifier { get; private set; }
    public RecordingModule Module { get; } = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
      string identifier,
      CancellationToken cancellationToken,
      object?[]? args)
    {
      Identifiers.Add(identifier);
      if (identifier == "import")
      {
        ImportedSpecifier = args is { Length: > 0 } ? args[0] as string : null;
        return ValueTask.FromResult((TValue)(object)Module);
      }

      throw new InvalidOperationException($"Unexpected identifier '{identifier}'.");
    }
  }

  private sealed class RecordingModule : IJSObjectReference
  {
    public List<string> ExportNames { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
      string identifier,
      CancellationToken cancellationToken,
      object?[]? args)
    {
      ExportNames.Add(identifier);
      string json = identifier switch
      {
        "CreateCredential" => """{"credentialId":"cid","clientDataJson":"cdj","attestationObject":"att"}""",
        "GetCredential" =>
          """{"credentialId":"cid","clientDataJson":"cdj","authenticatorData":"ad","signature":"sig","userHandle":null}""",
        _ => throw new InvalidOperationException($"Unexpected export '{identifier}'.")
      };
      return ValueTask.FromResult((TValue)(object)json);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }
}
