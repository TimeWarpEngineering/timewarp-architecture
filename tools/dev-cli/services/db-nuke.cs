#region Purpose
// Argument builders, AppHost volume-name resolution and the refusal text for `dev db nuke`.
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests can Compile-include them and gate
// the destructive verb without running Aspire or Docker.
//
// Volume resolution mirrors Aspire.Hosting 13.6 VolumeNameGenerator, the scheme WithDataVolume and
// the AppHost's WithVolume(env:) use: {sanitized application name}-{first 10 hex of
// SHA256(lower-cased full AppHost .csproj path)}-{resource}-{suffix}. The application name is the
// AppHost assembly name, which is the .csproj file name (the AppHost sets no AssemblyName). Every
// volume this AppHost names starts with that prefix, and the path hash keeps it per checkout: a
// sibling worktree's AppHost has a different prefix and is never listed or removed. Verified
// against a live dev volume (master checkout → aspire-app-host-bbe41f2e49-postgres-data).
//
// Why a Docker sweep follows `aspire stop --force --volumes`: Aspire removes only volumes it
// recorded as Aspire-owned. A volume that existed before the AppHost started (any dev volume
// created before Aspire 13.6 ownership records) is adopted, not owned, and Aspire leaves it
// intact. The sweep removes only the prefixed volumes resolved before acting (printed without
// --yes, and again by the --yes path before the stop), so every removed name was shown first.
#endregion

namespace DevCli.Services;

/// <summary>Builds the <c>aspire stop</c> / <c>docker volume</c> invocations for <c>dev db nuke</c>.</summary>
internal static class DbNuke
{
  /// <summary>First Aspire CLI version whose <c>aspire stop</c> accepts <c>--force --volumes</c>.</summary>
  internal static readonly Version MinimumCliVersion = new(13, 6);

  internal const string Requirement = "`dev db nuke` (aspire stop --force --volumes)";

  internal static string[] BuildStopArguments(string appHostProject) =>
    ["stop", "--apphost", appHostProject, "--force", "--volumes", "--non-interactive", "--nologo"];

  internal static string[] BuildVolumeListArguments(string volumePrefix) =>
    ["volume", "ls", "--quiet", "--filter", $"name={volumePrefix}"];

  internal static string[] BuildVolumeRemoveArguments(IEnumerable<string> volumes) =>
    ["volume", "rm", .. volumes];

  /// <summary>Name prefix shared by every volume the AppHost at <paramref name="appHostProjectPath"/> generates.</summary>
  internal static string VolumeNamePrefix(string appHostProjectPath)
  {
    string fullPath = Path.GetFullPath(appHostProjectPath);
    string applicationName = Sanitize(Path.GetFileNameWithoutExtension(fullPath)).ToLowerInvariant();
    byte[] hash = System.Security.Cryptography.SHA256.HashData(
      System.Text.Encoding.UTF8.GetBytes(fullPath.ToLowerInvariant()));
    string pathHash = Convert.ToHexString(hash)[..10].ToLowerInvariant();
    return $"{applicationName}-{pathHash}-";
  }

  /// <summary>
  /// Volume names from <c>docker volume ls --quiet</c> output that belong to this AppHost.
  /// Docker's name filter is a substring match, so the prefix is re-checked here.
  /// </summary>
  internal static string[] FilterAppHostVolumes(string dockerVolumeListOutput, string volumePrefix) =>
    [.. dockerVolumeListOutput
      .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Where(name => name.StartsWith(volumePrefix, StringComparison.Ordinal))
      .Order(StringComparer.Ordinal)];

  /// <summary>What <c>dev db nuke</c> prints (and refuses) when <c>--yes</c> is absent.</summary>
  internal static string[] BuildRefusalLines(string appHostProject, IReadOnlyCollection<string> volumes)
  {
    List<string> lines =
    [
      "Refusing to nuke without --yes. `dev db nuke --yes` would:",
      $"  1. Stop the running AppHost ({appHostProject}) if it is running.",
      volumes.Count == 0
        ? "  2. Remove this AppHost's Docker volumes. None exist right now."
        : $"  2. Permanently delete these {volumes.Count} Docker volume(s) and all data in them:",
    ];
    lines.AddRange(volumes.Select(volume => $"       {volume}"));
    lines.Add("The next `dev run` starts empty: migrations re-apply and seeds re-run.");
    lines.Add("To keep the volume and only wipe the schema inside a running AppHost, use `dev db reset --yes`.");
    return [.. lines];
  }

  // Same character rule as Aspire's VolumeNameGenerator.Sanitize: [a-zA-Z0-9][a-zA-Z0-9_.-]*,
  // anything else becomes '_'.
  private static string Sanitize(string name) =>
    string.Create(name.Length, name, static (span, text) =>
    {
      for (int i = 0; i < text.Length; i++)
      {
        char c = text[i];
        bool valid = char.IsAsciiLetterOrDigit(c) || (i > 0 && c is '_' or '.' or '-');
        span[i] = valid ? c : '_';
      }
    });
}
