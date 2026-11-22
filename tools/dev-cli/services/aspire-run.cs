#region Purpose
// Argument builder and launch-profile validation for `dev run`, plus the shared Aspire CLI version guard.
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests can Compile-include them and
// gate argument construction without launching an AppHost. ValidateCliVersion is the hard Aspire
// CLI minimum (dev run, dev publish, dev deploy, dev db nuke each pass their own).
// AssessCliVersion also warns when the installed CLI is older than <AspireVersion> in
// Directory.Packages.props — the one Aspire version, which the Aspire.* pins use. A lag warns and
// the verb continues; a CLI below the hard minimum still refuses.
// AspireCli.ValidateVersionAsync runs the `aspire --version` probe. The hard minimum on dev run
// is MinimumRunVersion, enforced on every dev run: since Aspire 17 (task 498) the AppHost is a
// Microsoft.NET.Sdk project that takes DCP and the dashboard from the Aspire CLI bundle
// (ASPIRE013), so an older CLI cannot run it. That minimum also covers --launch-profile (added in
// CLI 13.6). The repo-version warning applies to every dev run. Profile names
// come from launchSettings.json at run time — never a hard-coded list. JsonDocument keeps the
// launch-settings parse AOT-safe (no reflection serializer). The AspireVersion read is a string
// scan of the property element, also AOT-safe. The numeric core before '-' or '+' is the compared
// version, so a prerelease property still runs the verbs. A present value that is not a version is
// named in the refusal. The check that the pins, the AppHost, and CI agree on that version is
// AspireVersionSources (aspire-version-sources.cs).
#endregion

namespace DevCli.Services;

/// <summary>Builds and validates the <c>aspire run</c> invocation for <c>dev run</c>.</summary>
internal static class AspireRun
{
  internal const string AppHostProject =
    "source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj";

  internal const string LaunchSettingsFile =
    "source/container-apps/aspire/projects/aspire-app-host/Properties/launchSettings.json";

  /// <summary>
  /// First Aspire CLI whose bundle (DCP and the dashboard) runs this Aspire 17 AppHost. It also
  /// accepts <c>--launch-profile</c> (added in 13.6).
  /// </summary>
  internal static readonly Version MinimumRunVersion = new(17, 0);

  /// <summary>What <see cref="MinimumRunVersion"/> is required for, in the refusal text.</summary>
  internal const string RunRequirement =
    "`dev run` (the AppHost takes DCP and the dashboard from the Aspire CLI bundle)";

  internal const string DirectoryPackagesPropsFile = "Directory.Packages.props";

  internal const string ToolUpdateCommand = "dotnet tool update -g Aspire.Cli";

  internal const string UpdateHint =
    $"Update the Aspire CLI with `aspire update --self` or `{ToolUpdateCommand}`, then run `aspire setup` "
    + "so the AppHost build takes that CLI's bundle.";

  internal const string MissingRepoVersionMessage =
    "Could not read <AspireVersion> from Directory.Packages.props. That property is the repo's Aspire version.";

  /// <summary>Hard-minimum refusal, and a warning when the CLI is older than the repo Aspire version.</summary>
  internal readonly record struct CliVersionAssessment(string? Error, string? Warning);

  internal static string[] BuildRunArguments(string appHostProject, string? launchProfile) =>
    launchProfile is null
      ? ["run", "--apphost", appHostProject]
      : ["run", "--apphost", appHostProject, "--launch-profile", launchProfile];

  /// <summary>Profile names in declaration order from launchSettings.json content.</summary>
  internal static string[] ReadLaunchProfileNames(string launchSettingsJson)
  {
    using var document = System.Text.Json.JsonDocument.Parse(
      launchSettingsJson,
      new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });

    if (!document.RootElement.TryGetProperty("profiles", out System.Text.Json.JsonElement profiles)
      || profiles.ValueKind != System.Text.Json.JsonValueKind.Object)
    {
      return [];
    }

    return [.. profiles.EnumerateObject().Select(profile => profile.Name)];
  }

  /// <summary>Error message when <paramref name="launchProfile"/> is not declared; null when valid.</summary>
  internal static string? ValidateLaunchProfile(string launchProfile, IReadOnlyCollection<string> profileNames)
  {
    // launchSettings.json profile names are matched case-sensitively by dotnet run / aspire run.
    if (profileNames.Contains(launchProfile, StringComparer.Ordinal))
    {
      return null;
    }

    string valid = profileNames.Count == 0 ? "(none declared)" : string.Join(", ", profileNames);
    return $"Unknown launch profile '{launchProfile}'. Valid profiles in {LaunchSettingsFile}: {valid}";
  }

  /// <summary>Parses <c>aspire --version</c> output (e.g. <c>13.6.0+56f3e9c0</c>, <c>13.6.0-preview.1</c>).</summary>
  internal static Version? ParseCliVersion(string versionOutput) => ParseVersionCore(versionOutput);

  /// <summary>
  /// Error message when the installed CLI (from <c>aspire --version</c> output) is older than
  /// <paramref name="minimum"/> or unparseable; null when <paramref name="requirement"/> can run.
  /// Shared by every dev verb that needs a newer Aspire CLI (dev run -lp, dev db nuke).
  /// </summary>
  internal static string? ValidateCliVersion(string versionOutput, Version minimum, string requirement)
  {
    Version? version = ParseCliVersion(versionOutput);
    if (version is null)
    {
      return $"Could not determine the Aspire CLI version from `aspire --version` output '{versionOutput.Trim()}'. "
        + $"{requirement} requires Aspire CLI {minimum} or later. {UpdateHint}";
    }

    if (version < minimum)
    {
      return $"{requirement} requires Aspire CLI {minimum} or later (installed: {version}). "
        + UpdateHint;
    }

    return null;
  }

  /// <summary>
  /// <see cref="ValidateCliVersion"/> plus a warning when the parsed CLI is older than
  /// <paramref name="repoVersion"/>. The warning is set even when the hard minimum also fails,
  /// so a verb that does not enforce the minimum still reports the lag.
  /// </summary>
  internal static CliVersionAssessment AssessCliVersion(
    string versionOutput, Version minimum, string requirement, Version repoVersion)
  {
    string? error = ValidateCliVersion(versionOutput, minimum, requirement);
    Version? installed = ParseCliVersion(versionOutput);
    string? warning = installed is null ? null : LagWarning(installed, repoVersion);
    return new(error, warning);
  }

  /// <summary>Warning when <paramref name="installed"/> is older than <paramref name="repoVersion"/>; null otherwise.</summary>
  internal static string? LagWarning(Version installed, Version repoVersion)
  {
    if (installed >= repoVersion)
    {
      return null;
    }

    return $"Warning: Aspire CLI {installed} is older than this repo's Aspire {repoVersion}. "
      + $"Update with `{ToolUpdateCommand}`.";
  }

  /// <summary>The <c>AspireVersion</c> property in Directory.Packages.props text; null when it is missing or not a version.</summary>
  internal static Version? ParseRepoAspireVersion(string directoryPackagesProps) =>
    TryReadAspireVersionElement(directoryPackagesProps, out string value)
      ? ParseVersionCore(value)
      : null;

  /// <summary>
  /// Refusal when <paramref name="directoryPackagesProps"/> has no usable <c>AspireVersion</c>.
  /// Names a value that is present but not a version.
  /// </summary>
  internal static string DescribeMissingRepoAspireVersion(string directoryPackagesProps)
  {
    if (!TryReadAspireVersionElement(directoryPackagesProps, out string value))
    {
      return "Directory.Packages.props has no <AspireVersion> value.";
    }

    return $"<AspireVersion> is '{value}', which is not a version.";
  }

  /// <summary>
  /// Refusal for <see cref="ReadRepoAspireVersion"/> returning null.
  /// Names a property value that is present but not a version.
  /// </summary>
  internal static string ReadRepoAspireVersionError(string repoRoot)
  {
    string path = Path.Combine(repoRoot, DirectoryPackagesPropsFile);
    if (!File.Exists(path))
    {
      return MissingRepoVersionMessage;
    }

    string props = File.ReadAllText(path);
    if (!TryReadAspireVersionElement(props, out string value))
    {
      return MissingRepoVersionMessage;
    }

    return $"<AspireVersion> is '{value}', which is not a version. That property is the repo's Aspire version.";
  }

  /// <summary>Reads <see cref="ParseRepoAspireVersion"/> from <paramref name="repoRoot"/>; null when the file or property is absent.</summary>
  internal static Version? ReadRepoAspireVersion(string repoRoot)
  {
    string path = Path.Combine(repoRoot, DirectoryPackagesPropsFile);
    return File.Exists(path) ? ParseRepoAspireVersion(File.ReadAllText(path)) : null;
  }

  /// <summary>Numeric core before the first <c>+</c>, <c>-</c>, or whitespace.</summary>
  private static Version? ParseVersionCore(string text)
  {
    string trimmed = text.Trim();
    int end = trimmed.IndexOfAny(['+', '-', ' ', '\t', '\r', '\n']);
    string core = end < 0 ? trimmed : trimmed[..end];
    return Version.TryParse(core, out Version? version) ? version : null;
  }

  private static bool TryReadAspireVersionElement(string directoryPackagesProps, out string value)
  {
    value = "";
    const string open = "<AspireVersion>";
    const string close = "</AspireVersion>";
    int start = directoryPackagesProps.IndexOf(open, StringComparison.Ordinal);
    if (start < 0)
    {
      return false;
    }

    start += open.Length;
    int end = directoryPackagesProps.IndexOf(close, start, StringComparison.Ordinal);
    if (end < 0)
    {
      return false;
    }

    value = directoryPackagesProps[start..end].Trim();
    return true;
  }
}
