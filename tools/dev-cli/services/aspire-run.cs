#region Purpose
// Argument builder, launch-profile validation and CLI version guard for `dev run`.
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests can Compile-include them and
// gate argument construction without launching an AppHost. Profile names come from the
// AppHost's Properties/launchSettings.json at run time — never a hard-coded list. The version
// guard only applies when --launch-profile is given: Aspire CLI 13.6 added -lp to `aspire run`;
// older CLIs would reject it with an opaque parse error, so `dev run` fails first with the update
// command. JsonDocument keeps the parse AOT-safe (no reflection serializer).
#endregion

namespace DevCli.Services;

/// <summary>Builds and validates the <c>aspire run</c> invocation for <c>dev run</c>.</summary>
internal static class AspireRun
{
  internal const string AppHostProject =
    "source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj";

  internal const string LaunchSettingsFile =
    "source/container-apps/aspire/projects/aspire-app-host/Properties/launchSettings.json";

  /// <summary>First Aspire CLI version whose <c>aspire run</c> accepts <c>--launch-profile</c>.</summary>
  internal static readonly Version MinimumLaunchProfileVersion = new(13, 6);

  internal const string UpdateHint =
    "Update the Aspire CLI with `aspire update --self` or `dotnet tool update -g Aspire.Cli`.";

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
  internal static Version? ParseCliVersion(string versionOutput)
  {
    string text = versionOutput.Trim();
    int end = text.IndexOfAny(['+', '-', ' ', '\n', '\r']);
    string core = end < 0 ? text : text[..end];
    return Version.TryParse(core, out Version? version) ? version : null;
  }

  /// <summary>Error message when the installed CLI cannot take --launch-profile; null when it can.</summary>
  internal static string? ValidateCliVersionForLaunchProfile(string versionOutput)
  {
    Version? version = ParseCliVersion(versionOutput);
    if (version is null)
    {
      return $"Could not determine the Aspire CLI version from `aspire --version` output '{versionOutput.Trim()}'. "
        + $"--launch-profile requires Aspire CLI {MinimumLaunchProfileVersion} or later. {UpdateHint}";
    }

    if (version < MinimumLaunchProfileVersion)
    {
      return $"--launch-profile requires Aspire CLI {MinimumLaunchProfileVersion} or later (installed: {version}). "
        + UpdateHint;
    }

    return null;
  }
}
