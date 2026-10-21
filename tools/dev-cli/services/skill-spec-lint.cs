#region Purpose
// The pinned vally CLI, the arguments `dev lint-skills` runs, and the check that CI calls that verb.
#endregion

#region Design
// tools/dev-cli/vally-cli.version is the one vally version. CI reads it and installs
// @microsoft/vally-cli at that version, then runs `dev lint-skills`. The verb runs
// `vally lint skills` and `vally lint --eval-spec` for each skills/*/evals/eval.yaml.
// A workflow that installs an unpinned package, or that shells out to `vally lint` itself,
// fails WorkflowDriftError. dev build does not run this. ganda repo audit is not extended
// from this repo.
#endregion

namespace DevCli.Services;

using System.Text.RegularExpressions;

/// <summary>Skill-spec lint shared by <c>dev lint-skills</c> and the CI agreement check.</summary>
internal static partial class SkillSpecLint
{
  internal const string VersionFileRelativePath = "tools/dev-cli/vally-cli.version";

  internal const string PackageName = "@microsoft/vally-cli";

  internal const string LintSkillsCommand = "dotnet run tools/dev-cli/dev.cs -- lint-skills";

  internal static readonly string[] SkillLintArguments = ["lint", "skills"];

  /// <summary><c>npm install -g @microsoft/vally-cli@&lt;version&gt;</c> for <paramref name="pinnedVersion"/>.</summary>
  internal static string InstallCommand(string pinnedVersion) =>
    $"npm install -g {PackageName}@{pinnedVersion}";

  /// <summary>Refusal when <c>vally</c> is not on <c>PATH</c>. Names Node.js and the pinned install.</summary>
  internal static string MissingExecutableMessage(string pinnedVersion) =>
    "vally is not on PATH. Skill spec lint needs Node.js and the pinned CLI "
    + $"({VersionFileRelativePath}). Install Node.js (https://nodejs.org/), then:\n"
    + InstallCommand(pinnedVersion);

  /// <summary>Refusal when the <c>vally</c> on <c>PATH</c> is not <paramref name="pinnedVersion"/>.</summary>
  internal static string VersionMismatchMessage(string? actual, string pinnedVersion)
  {
    string found = actual is null
      ? "vally --version did not report a version"
      : $"vally {actual} is on PATH";
    return found
      + $". This repo pins {PackageName}@{pinnedVersion} ({VersionFileRelativePath}). Install that version with:\n"
      + InstallCommand(pinnedVersion);
  }

  /// <summary>Trimmed pin from <see cref="VersionFileRelativePath"/>, or null when the file is missing or not a version.</summary>
  internal static string? ReadPinnedVersion(string repoRoot)
  {
    string path = Path.Combine(repoRoot, VersionFileRelativePath);
    if (!File.Exists(path))
    {
      return null;
    }

    string text = File.ReadAllText(path).Trim();
    string? version = ParseVallyVersion(text);
    return version is not null && string.Equals(version, text, StringComparison.Ordinal) ? version : null;
  }

  /// <summary>The first semantic version in <paramref name="text"/>, or null.</summary>
  internal static string? ParseVallyVersion(string? text)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      return null;
    }

    foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      Match match = VersionPattern().Match(line);
      if (match.Success)
      {
        return match.Groups["version"].Value;
      }
    }

    return null;
  }

  /// <summary>Repo-relative <c>skills/*/evals/eval.yaml</c> paths, forward slashes, ordinal order.</summary>
  internal static IReadOnlyList<string> EvalSpecRelativePaths(string repoRoot)
  {
    string skillsDirectory = Path.Combine(repoRoot, "skills");
    if (!Directory.Exists(skillsDirectory))
    {
      return [];
    }

    return Directory.GetDirectories(skillsDirectory)
      .Select(directory => Path.Combine(directory, "evals", "eval.yaml"))
      .Where(File.Exists)
      .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
      .OrderBy(path => path, StringComparer.Ordinal)
      .ToArray();
  }

  /// <summary>Arguments for <c>vally lint --eval-spec &lt;relativeSpec&gt;</c>.</summary>
  internal static string[] EvalLintArguments(string relativeSpec) =>
    ["lint", "--eval-spec", relativeSpec];

  /// <summary>True when <paramref name="executable"/> is a file on <paramref name="path"/>.</summary>
  internal static bool IsOnPath(string executable, string? path) =>
    (path ?? "")
      .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
      .Any(directory =>
        File.Exists(Path.Combine(directory, executable))
        || File.Exists(Path.Combine(directory, executable + ".exe")));

  /// <summary>
  /// Null when CI reads <see cref="VersionFileRelativePath"/>, installs that package version,
  /// runs <see cref="LintSkillsCommand"/>, and does not invoke <c>vally lint</c> itself.
  /// Skill-lint also runs when <c>tools/dev-cli/</c> changes.
  /// </summary>
  internal static string? WorkflowDriftError(string workflowYaml)
  {
    string code = WithoutYamlComments(workflowYaml);
    if (!code.Contains(VersionFileRelativePath, StringComparison.Ordinal))
    {
      return $"CI must read the vally pin from {VersionFileRelativePath}.";
    }

    if (!code.Contains($"{PackageName}@$version", StringComparison.Ordinal))
    {
      return $"CI must install {PackageName} at the version read from {VersionFileRelativePath}.";
    }

    if (!VersionAssignmentReadsPinFile(code))
    {
      return $"CI must assign the vally version from {VersionFileRelativePath}.";
    }

    if (!code.Contains("^tools/dev-cli/", StringComparison.Ordinal))
    {
      return "CI must run skill lint when tools/dev-cli/ changes.";
    }

    if (!code.Contains(LintSkillsCommand, StringComparison.Ordinal))
    {
      return "CI must lint skill specs with `dev lint-skills`.";
    }

    foreach (string line in code.Split('\n'))
    {
      if (line.Contains("vally lint", StringComparison.Ordinal))
      {
        return "CI must not run `vally lint` itself. `dev lint-skills` is the lint.";
      }
    }

    return null;
  }

  private static bool VersionAssignmentReadsPinFile(string code)
  {
    const string needle = "version=";
    int at = 0;
    while (true)
    {
      at = code.IndexOf(needle, at, StringComparison.Ordinal);
      if (at < 0)
      {
        return false;
      }

      if (at > 0 && IsNameCharacter(code[at - 1]))
      {
        at += needle.Length;
        continue;
      }

      int lineEnd = code.IndexOf('\n', at, StringComparison.Ordinal);
      string assignment = lineEnd < 0 ? code[at..] : code[at..lineEnd];
      if (assignment.Contains(VersionFileRelativePath, StringComparison.Ordinal))
      {
        return true;
      }

      at += needle.Length;
    }
  }

  private static bool IsNameCharacter(char value) =>
    char.IsAsciiLetterOrDigit(value) || value is '_' or '-';

  private static string WithoutYamlComments(string text)
  {
    List<string> lines = [];
    foreach (string line in text.Split('\n'))
    {
      lines.Add(StripUnquotedHashComment(line));
    }

    return string.Join('\n', lines);
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

  [GeneratedRegex(@"(?:^|\s)v?(?<version>\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?)(?:\s|$)")]
  private static partial Regex VersionPattern();
}
