#region Purpose
// Checks that the Aspire package pins, the AppHost's Aspire packages, and the CI Aspire.Cli install
// all take the one Aspire version, <AspireVersion> in Directory.Packages.props.
#endregion

#region Design
// Pure text scans (no Amuru/Terminal, no MSBuild evaluation), AOT-safe, so dev-cli-tests
// Compile-include this file and run the check against the repo's own files. Split out of
// aspire-run.cs (task 498, TWA0038 line cap). Package pins must start with $(AspireVersion); a
// preview suffix after it is allowed. The AppHost check refuses the obsolete Aspire.AppHost.Sdk
// (ASPIRE012), requires Aspire.Hosting.AppHost and Aspire.Hosting.Dotnet, and refuses an Aspire
// PackageReference that carries its own Version or VersionOverride. The workflow check ignores '#'
// comments and requires `Aspire.Cli --version "$version"` where that variable is the sed read of
// the property in Directory.Packages.props. XML comments are stripped first, so a comment can name
// a rejected form without counting as one.
#endregion

namespace DevCli.Services;

/// <summary>Agreement check for the one Aspire version across pins, the AppHost, and CI.</summary>
internal static class AspireVersionSources
{
  /// <summary>
  /// Null when the AppHost's Aspire packages, the Aspire package pins, and the CI Aspire.Cli install
  /// all take <c>AspireVersion</c> from Directory.Packages.props. The first disagreement otherwise.
  /// </summary>
  internal static string? Validate(
    string directoryPackagesProps, string appHostProject, string workflowYaml)
  {
    if (AspireRun.ParseRepoAspireVersion(directoryPackagesProps) is null)
    {
      return AspireRun.DescribeMissingRepoAspireVersion(directoryPackagesProps);
    }

    return AspirePackagePinError(directoryPackagesProps)
      ?? AppHostPackagesError(appHostProject)
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

  private static string? AppHostPackagesError(string appHostProject)
  {
    appHostProject = WithoutXmlComments(appHostProject);
    if (appHostProject.Contains("Aspire.AppHost.Sdk", StringComparison.Ordinal))
    {
      return "AppHost uses the obsolete Aspire.AppHost.Sdk (ASPIRE012). Use Microsoft.NET.Sdk with "
        + "<PackageReference Include=\"Aspire.Hosting.AppHost\" /> and <PackageReference Include=\"Aspire.Hosting.Dotnet\" />.";
    }

    foreach (string package in AppHostRequiredPackages)
    {
      if (!appHostProject.Contains($"<PackageReference Include=\"{package}\"", StringComparison.Ordinal))
      {
        return $"AppHost must reference {package} (<PackageReference Include=\"{package}\" />).";
      }
    }

    const string marker = "<PackageReference Include=\"Aspire.";
    int search = 0;
    while (true)
    {
      int include = appHostProject.IndexOf(marker, search, StringComparison.Ordinal);
      if (include < 0)
      {
        return null;
      }

      int tagEnd = appHostProject.IndexOf('>', include, StringComparison.Ordinal);
      if (tagEnd < 0)
      {
        return "An AppHost Aspire PackageReference element is not closed.";
      }

      string tag = appHostProject[include..tagEnd];
      if (tag.Contains(" Version=", StringComparison.Ordinal) || tag.Contains("VersionOverride=", StringComparison.Ordinal))
      {
        return $"AppHost Aspire PackageReference '{tag}' sets its own version. Take it from the $(AspireVersion) pin in Directory.Packages.props.";
      }

      search = tagEnd + 1;
    }
  }

  /// <summary>The packages a Microsoft.NET.Sdk AppHost needs since Aspire 17 (AppHost targets, AddDotnetProject).</summary>
  private static readonly string[] AppHostRequiredPackages = ["Aspire.Hosting.AppHost", "Aspire.Hosting.Dotnet"];

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
