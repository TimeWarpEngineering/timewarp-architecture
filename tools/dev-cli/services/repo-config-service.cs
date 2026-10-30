#region Purpose
// Replaces the TimeWarp.Nuru.DevCli content copy of RepoConfigService for this runfile.
#endregion

#region Design
// DevCli 4.0.0-beta.9 compiles content/any/services/repo-config-service.cs into this runfile.
// That copy calls JsonSerializerOptions.GetTypeInfo(Type). Warnings are errors, and CA2263
// requires GetTypeInfo<RepoConfig>(). The package type is not partial, so this file is the
// whole type. Directory.Build.targets drops the packaged Compile item. The namespace stays
// DevCli so the packaged endpoints still bind IRepoConfigService to this class. Delete this
// file and OmitPackagedRepoConfigService when the pin uses the generic overload.
#endregion

namespace DevCli;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Reads per-repo configuration from <c>.timewarp/dev.jsonc</c>.
/// </summary>
public sealed class RepoConfigService : IRepoConfigService
{
  private const string ConfigFileName = ".timewarp/dev.jsonc";

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    TypeInfoResolver = DevCliJsonContext.Default
  };

  /// <inheritdoc />
  public async ValueTask<RepoConfig> GetConfigAsync(CancellationToken cancellationToken = default)
  {
    string? repoRoot = Git.FindRoot();

    if (repoRoot is null)
    {
      return new RepoConfig();
    }

    string configPath = Path.Combine(repoRoot, ConfigFileName);

    if (!File.Exists(configPath))
    {
      return new RepoConfig();
    }

    string json = await File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);

    return Parse(json);
  }

  /// <summary>
  /// Deserializes a <see cref="RepoConfig"/> from raw <c>.timewarp/dev.jsonc</c> content.
  /// Unknown keys are ignored. Returns defaults when the JSON deserializes to <c>null</c>.
  /// </summary>
  public static RepoConfig Parse(string json)
  {
    // GetTypeInfo<T> stays on the source-generated resolver. Deserialize<T>(string, options)
    // is RequiresUnreferencedCode (IL2026/IL3050). The non-generic GetTypeInfo(Type) is CA2263.
    JsonTypeInfo<RepoConfig> typeInfo = JsonOptions.GetTypeInfo<RepoConfig>();
    RepoConfig? config = JsonSerializer.Deserialize(json, typeInfo);

    return config ?? new RepoConfig();
  }
}
