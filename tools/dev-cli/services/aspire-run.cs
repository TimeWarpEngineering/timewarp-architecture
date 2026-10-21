#region Purpose
// Argument builder and launch-profile validation for `dev run`, plus the shared Aspire CLI version guard.
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests can Compile-include them and
// gate argument construction without launching an AppHost. ValidateCliVersion is the hard Aspire
// CLI minimum (dev run -lp, dev publish, dev deploy, dev db nuke each pass their own).
// AssessCliVersion also warns when the installed CLI is older than <AspireVersion> in
// Directory.Packages.props — the one Aspire version, which the AppHost SDK and the Aspire.* pins
// use. A lag warns and the verb continues; a CLI below the hard minimum still refuses.
// AspireCli.ValidateVersionAsync runs the `aspire --version` probe. The hard minimum on dev run
// applies only with --launch-profile (Aspire CLI 13.6 added -lp; an older CLI would reject it
// with an opaque parse error). The repo-version warning applies to every dev run. Profile names
// come from launchSettings.json at run time — never a hard-coded list. JsonDocument keeps the
// launch-settings parse AOT-safe (no reflection serializer). The AspireVersion read is a string
// scan of the property element, also AOT-safe. The numeric core before '-' or '+' is the compared
// version, so a prerelease property still runs the verbs. A present value that is not a version is
// named in the refusal. The workflow check ignores '#' comments and requires
// `Aspire.Cli --version "$version"` where that variable is the sed read of the property in
// Directory.Packages.props.
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

  internal const string DirectoryPackagesPropsFile = "Directory.Packages.props";

  internal const string ToolUpdateCommand = "dotnet tool update -g Aspire.Cli";

  internal const string UpdateHint =
    $"Update the Aspire CLI with `aspire update --self` or `{ToolUpdateCommand}`.";

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

  /// <summary>
  /// Null when the AppHost SDK, the Aspire package pins, and the CI Aspire.Cli install all take
  /// <c>AspireVersion</c> from Directory.Packages.props. The first disagreement otherwise.
  /// </summary>
  internal static string? ValidateAspireVersionSources(
    string directoryPackagesProps, string appHostProject, string workflowYaml)
  {
    if (ParseRepoAspireVersion(directoryPackagesProps) is null)
    {
      return DescribeMissingRepoAspireVersion(directoryPackagesProps);
    }

    return AspirePackagePinError(directoryPackagesProps)
      ?? AppHostSdkError(appHostProject)
      ?? WorkflowCliInstallError(workflowYaml);
  }

  private static string? AspirePackagePinError(string directoryPackagesProps)
  {
    const string marker = "Include=\"Aspire.";
    int search = 0;
    while (true)
    {
      int include = directoryPackagesProps.IndexOf(marker, search, StringComparison.Ordinal);
      if (include < 0)
      {
        return null;
      }

      int tagEnd = directoryPackagesProps.IndexOf("/>", include, StringComparison.Ordinal);
      if (tagEnd < 0)
      {
        return "An Aspire PackageVersion element is not closed.";
      }

      string tag = directoryPackagesProps[include..tagEnd];
      const string versionAttribute = "Version=\"";
      int versionKey = tag.IndexOf(versionAttribute, StringComparison.Ordinal);
      if (versionKey < 0)
      {
        return "An Aspire package pin has no Version.";
      }

      int start = versionKey + versionAttribute.Length;
      int end = tag.IndexOf('"', start, StringComparison.Ordinal);
      if (end < 0)
      {
        return "An Aspire package pin has an unclosed Version.";
      }

      string version = tag[start..end];
      if (!version.StartsWith("$(AspireVersion)", StringComparison.Ordinal))
      {
        return $"Aspire package pin '{version}' must use $(AspireVersion).";
      }

      search = tagEnd + 2;
    }
  }

  private static string? AppHostSdkError(string appHostProject)
  {
    appHostProject = WithoutXmlComments(appHostProject);
    if (appHostProject.Contains("Aspire.AppHost.Sdk/", StringComparison.Ordinal))
    {
      return "AppHost SDK version is a literal. Use <Sdk Name=\"Aspire.AppHost.Sdk\" Version=\"$(AspireVersion)\" />.";
    }

    if (!appHostProject.Contains("Name=\"Aspire.AppHost.Sdk\"", StringComparison.Ordinal)
      || !appHostProject.Contains("Version=\"$(AspireVersion)\"", StringComparison.Ordinal))
    {
      return "AppHost SDK must be <Sdk Name=\"Aspire.AppHost.Sdk\" Version=\"$(AspireVersion)\" />.";
    }

    return null;
  }

  private static string? WorkflowCliInstallError(string workflowYaml)
  {
    string code = WithoutYamlComments(workflowYaml);
    bool foundInstall = false;
    foreach (string line in code.Split('\n'))
    {
      if (!line.Contains("dotnet tool install", StringComparison.Ordinal)
        || !line.Contains("Aspire.Cli", StringComparison.Ordinal))
      {
        continue;
      }

      foundInstall = true;
      string? error = AspireCliInstallLineError(code, line);
      if (error is not null)
      {
        return error;
      }
    }

    return foundInstall
      ? null
      : "CI must install Aspire.Cli with --version \"$version\" read from <AspireVersion> in Directory.Packages.props.";
  }

  private static string? AspireCliInstallLineError(string code, string install)
  {
    const string versionFlag = "--version";
    int flag = install.IndexOf(versionFlag, StringComparison.Ordinal);
    if (flag < 0)
    {
      return "CI installs Aspire.Cli without --version. Pass --version \"$version\" from <AspireVersion> in Directory.Packages.props.";
    }

    string after = install[(flag + versionFlag.Length)..].TrimStart();
    if (after.Length > 0 && char.IsAsciiDigit(after[0]))
    {
      return "CI installs Aspire.Cli at a literal version. Read <AspireVersion> from Directory.Packages.props.";
    }

    if (!TryReadShellVariable(after, out string variable) || !ShellVariableReadsAspireVersion(code, variable))
    {
      return "CI installs Aspire.Cli at a version that does not come from <AspireVersion> in Directory.Packages.props.";
    }

    return null;
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

  private static bool TryReadShellVariable(string after, out string variable)
  {
    variable = "";
    if (after.Length == 0)
    {
      return false;
    }

    if (after[0] == '"')
    {
      if (after.Length < 3 || after[1] != '$')
      {
        return false;
      }

      int close = after.IndexOf('"', 2, StringComparison.Ordinal);
      if (close < 0)
      {
        return false;
      }

      variable = after[2..close];
      return IsShellName(variable);
    }

    if (after[0] != '$' || after.Length < 2 || !IsShellNameChar(after[1]))
    {
      return false;
    }

    int index = 1;
    while (index < after.Length && IsShellNameChar(after[index]))
    {
      index++;
    }

    variable = after[1..index];
    return true;
  }

  private static bool ShellVariableReadsAspireVersion(string code, string variable)
  {
    string needle = variable + "=";
    int at = 0;
    while (true)
    {
      at = code.IndexOf(needle, at, StringComparison.Ordinal);
      if (at < 0)
      {
        return false;
      }

      if (at > 0 && IsShellNameChar(code[at - 1]))
      {
        at += needle.Length;
        continue;
      }

      string rest = code[(at + needle.Length)..];
      int lineEnd = rest.IndexOf('\n', StringComparison.Ordinal);
      string assignment = lineEnd < 0 ? rest : rest[..lineEnd];
      if (assignment.Contains("sed", StringComparison.Ordinal)
        && assignment.Contains("<AspireVersion>", StringComparison.Ordinal)
        && assignment.Contains("Directory.Packages.props", StringComparison.Ordinal))
      {
        return true;
      }

      at += needle.Length;
    }
  }

  private static string WithoutYamlComments(string text)
  {
    System.Text.StringBuilder builder = new(text.Length);
    foreach (string line in text.Split('\n'))
    {
      builder.Append(StripUnquotedHashComment(line));
      builder.Append('\n');
    }

    return builder.ToString();
  }

  private static string StripUnquotedHashComment(string line)
  {
    bool inSingle = false;
    bool inDouble = false;
    for (int index = 0; index < line.Length; index++)
    {
      char current = line[index];
      if (current == '\'' && !inDouble)
      {
        inSingle = !inSingle;
      }
      else if (current == '"' && !inSingle)
      {
        inDouble = !inDouble;
      }
      else if (current == '#' && !inSingle && !inDouble)
      {
        return line[..index];
      }
    }

    return line;
  }

  private static bool IsShellName(string name)
  {
    if (name.Length == 0)
    {
      return false;
    }

    foreach (char character in name)
    {
      if (!IsShellNameChar(character))
      {
        return false;
      }
    }

    return true;
  }

  private static bool IsShellNameChar(char value) =>
    value == '_' || char.IsAsciiLetterOrDigit(value);

  /// <summary>XML with comments removed, so a comment can name a rejected form without counting as a pin.</summary>
  private static string WithoutXmlComments(string xml)
  {
    System.Text.StringBuilder builder = new(xml.Length);
    int index = 0;
    while (index < xml.Length)
    {
      int open = xml.IndexOf("<!--", index, StringComparison.Ordinal);
      if (open < 0)
      {
        builder.Append(xml, index, xml.Length - index);
        break;
      }

      builder.Append(xml, index, open - index);
      int close = xml.IndexOf("-->", open + 4, StringComparison.Ordinal);
      if (close < 0)
      {
        break;
      }

      index = close + 3;
    }

    return builder.ToString();
  }
}
