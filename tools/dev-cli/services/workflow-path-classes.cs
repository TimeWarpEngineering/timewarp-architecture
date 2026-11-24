#region Purpose
// Which CI jobs a changed path turns on: skill lint and template smoke. `dev workflow --phase
// classify` writes the answers (with ci from PullRequestBuildClassifier) to GITHUB_OUTPUT.
#endregion

#region Design
// Pure string rules (no Amuru, no git), so dev-cli-tests Compile-include this file. Classify is
// the whole classify decision: an unknown diff (null) turns on every job; a known diff takes ci
// from the build rule the caller passes (PullRequestBuildClassifier.RequiresBuild with
// .timewarp/dev.jsonc pullRequestBuild, content of TimeWarp.Nuru.DevCli that dev-cli-tests do not
// compile) and skill and template from the rules below. Push and pull request use the same rule.
// CI roots (.github/workflows/, .github/actions/) and tools/dev-cli/ turn on every job: they change
// how CI itself runs, and dev lint-skills and dev template-smoke live in tools/dev-cli/.
// Skill lint: SKILL.md or eval.yaml anywhere, .vally.yaml, evals/, skills/.
// Template smoke: the template content roots (source/, tests/, timewarp-templates/,
// .template.config/, msbuild/) and the root build files the generated app inherits.
// A push event carries before/after SHAs; an all-zero before (a new branch) has no base, and the
// caller treats an unknown diff as "every job runs".
#endregion

namespace DevCli.Services;

using System.Text.Json;

/// <summary>Skill-lint and template-smoke path rules for <c>dev workflow --phase classify</c>.</summary>
internal static class WorkflowPathClasses
{
  /// <summary>Roots whose change turns on every CI job.</summary>
  internal static readonly string[] CiRoots = [".github/workflows/", ".github/actions/", "tools/dev-cli/"];

  private static readonly string[] SkillRoots = ["evals/", "skills/"];

  private static readonly string[] TemplateRoots =
    ["source/", "tests/", "timewarp-templates/", ".template.config/", "msbuild/"];

  private static readonly string[] TemplateRootFiles =
    ["Directory.Build.props", "Directory.Packages.props", "timewarp-architecture.slnx"];

  /// <summary>
  /// The classify outputs. A null <paramref name="changed"/> (diff not read) turns on every job;
  /// otherwise ci is <paramref name="requiresBuild"/> over the diff.
  /// </summary>
  internal static WorkflowJobs Classify(IReadOnlyList<string>? changed, Func<IReadOnlyList<string>, bool> requiresBuild)
  {
    ArgumentNullException.ThrowIfNull(requiresBuild);
    if (changed is null) return new WorkflowJobs(Ci: true, Skill: true, Template: true);

    return new WorkflowJobs(requiresBuild(changed), changed.Any(IsSkillPath), changed.Any(IsTemplatePath));
  }

  /// <summary>True when <paramref name="path"/> should run the skill-lint job.</summary>
  internal static bool IsSkillPath(string path)
  {
    path = Normalize(path);
    string name = path.Split('/')[^1];
    return name is "SKILL.md" or "eval.yaml"
      || path == ".vally.yaml"
      || StartsWithAny(path, SkillRoots)
      || StartsWithAny(path, CiRoots);
  }

  /// <summary>True when <paramref name="path"/> should run the template-smoke job.</summary>
  internal static bool IsTemplatePath(string path)
  {
    path = Normalize(path);
    return StartsWithAny(path, TemplateRoots)
      || TemplateRootFiles.Contains(path, StringComparer.Ordinal)
      || StartsWithAny(path, CiRoots);
  }

  /// <summary>
  /// Reads a push event's <c>before</c> and <c>after</c>. False when either is missing, not a SHA,
  /// or <c>before</c> is all zeros (a new branch has no base to diff).
  /// </summary>
  internal static bool TryReadPushShas(string? json, out string before, out string after)
  {
    before = "";
    after = "";
    if (string.IsNullOrWhiteSpace(json)) return false;

    try
    {
      using var document = JsonDocument.Parse(json);
      JsonElement root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object
        || !TryReadSha(root, "before", out string baseSha)
        || !TryReadSha(root, "after", out string headSha)
        || baseSha.All(character => character == '0'))
      {
        return false;
      }

      before = baseSha;
      after = headSha;
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private static bool TryReadSha(JsonElement root, string name, out string sha)
  {
    sha = "";
    if (!root.TryGetProperty(name, out JsonElement element) || element.ValueKind != JsonValueKind.String)
    {
      return false;
    }

    string value = element.GetString() ?? "";
    if (value.Length is < 7 or > 64 || !value.All(char.IsAsciiHexDigit)) return false;

    sha = value;
    return true;
  }

  private static string Normalize(string path)
  {
    path = path.Replace('\\', '/').Trim();
    return path.StartsWith("./", StringComparison.Ordinal) ? path[2..] : path;
  }

  private static bool StartsWithAny(string path, string[] roots) =>
    roots.Any(root => path.StartsWith(root, StringComparison.Ordinal));
}

/// <summary>Which path-gated CI jobs run: ci-group, skill-lint, template-smoke.</summary>
internal readonly record struct WorkflowJobs(bool Ci, bool Skill, bool Template);
