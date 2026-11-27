#region Purpose
// Which CI jobs a changed path turns on: skill lint, template smoke, and which ci-group legs run.
// `dev workflow --phase classify` writes the answers (with ci from PullRequestBuildClassifier) to
// GITHUB_OUTPUT.
#endregion

#region Design
// Pure string rules (no Amuru, no git), so dev-cli-tests Compile-include this file. Classify is
// the whole classify decision: an unknown diff (null) turns on every job; a known diff takes ci
// from the build rule the caller passes (PullRequestBuildClassifier.RequiresBuild with
// .timewarp/dev.jsonc pullRequestBuild, content of TimeWarp.Nuru.DevCli that dev-cli-tests do not
// compile) and skill and template from the rules below. Push and pull request use the same rule.
// CI roots (.github/workflows/, .github/actions/) turn on every job: they change how CI itself runs.
// Skill lint: SKILL.md or eval.yaml anywhere, .vally.yaml, evals/, skills/, and the dev CLI files
// `dev lint-skills` runs (SkillDevCliPaths: the verb, the vally pin, the spec lint, the client-name
// scan it runs first, and the CLI host files every verb compiles with). Any other tools/dev-cli/
// edit (release, pack, deploy, workflow services) does not lint skills (task 534).
// Template smoke: the template content roots (source/, tests/, timewarp-templates/,
// .template.config/, msbuild/), all of tools/dev-cli/ (generated apps ship the dev CLI), the root
// files the generated app inherits, including the dev CLI's .timewarp/dev.jsonc and .envrc (task
// 530), and the generated app's workflow under .github/app-workflows/ (task 531).
//
// ci-group legs (task 534). ClassifyCiLegs decides whether ci (integration) and ci (publish) run
// beside ci (fast), which always runs when ci is on. Only a pull request with a known diff narrows:
// a push (master merges stage Packages-* from ci (publish)), a dispatch, and an unknown diff run
// every leg. A pull request skips a leg when every changed path is either a non-build path (the
// caller's build rule, over the rest) or fast-only: tools/dev-cli/ and tests/tools/dev-cli-tests/,
// whose tests run in the fast group. A dev CLI file the leg itself runs turns it back on:
// IntegrationDevCliPaths (the test verb and its services) for integration, PublishDevCliPaths
// (publish verbs and Aspire publish targets) for publish, HostDevCliPaths (dev.cs, the props, the
// global usings, and workflow-command.cs, which runs both phases) for both. The lists name direct
// entry points, not every transitive helper; the master push after the merge runs every leg, so a
// missed helper fails there, not in production. A run that skipped a leg is not a full ci pass:
// its classify writes ci_marker=false, so workflow.yml's green-key job uploads no ci marker and the
// master push cannot skip ci-group on that key.
// A push event carries before/after SHAs; an all-zero before (a new branch) has no base, and the
// caller treats an unknown diff as "every job runs".
#endregion

namespace DevCli.Services;

using System.Text.Json;

/// <summary>Skill-lint and template-smoke path rules for <c>dev workflow --phase classify</c>.</summary>
internal static class WorkflowPathClasses
{
  /// <summary>Roots whose change turns on every CI job.</summary>
  internal static readonly string[] CiRoots = [".github/workflows/", ".github/actions/"];

  /// <summary>The ci-group legs in matrix order.</summary>
  internal static readonly string[] CiLegNames = ["fast", "integration", "publish"];

  private const string DevCliRoot = "tools/dev-cli/";

  private static readonly string[] SkillRoots = ["evals/", "skills/"];

  // Dev CLI files every verb compiles with or that run the ci-group phases.
  private static readonly string[] HostDevCliPaths =
  [
    "tools/dev-cli/dev.cs",
    "tools/dev-cli/Directory.Build.props",
    "tools/dev-cli/global-usings.cs",
    "tools/dev-cli/endpoints/workflow-command.cs",
  ];

  private static readonly string[] SkillDevCliPaths =
  [
    "tools/dev-cli/vally-cli.version",
    "tools/dev-cli/endpoints/lint-skills",
    "tools/dev-cli/services/skill-spec-lint",
    "tools/dev-cli/endpoints/check-client-names",
    "tools/dev-cli/services/client-name-scan",
  ];

  private static readonly string[] IntegrationDevCliPaths =
  [
    "tools/dev-cli/endpoints/test-command",
    "tools/dev-cli/services/test-",
    "tools/dev-cli/services/dotnet-test-arguments",
    "tools/dev-cli/services/jaribu-aggregator-summary-gate",
  ];

  private static readonly string[] PublishDevCliPaths =
  [
    "tools/dev-cli/endpoints/publish-",
    "tools/dev-cli/services/publish-",
    "tools/dev-cli/services/aspire-publish",
  ];

  // Paths whose tests run in ci (fast) only.
  private static readonly string[] FastOnlyRoots = [DevCliRoot, "tests/tools/dev-cli-tests/"];

  private static readonly string[] TemplateRoots =
    ["source/", "tests/", "timewarp-templates/", ".template.config/", "msbuild/", ".github/app-workflows/", DevCliRoot];

  private static readonly string[] TemplateRootFiles =
    ["Directory.Build.props", "Directory.Packages.props", "timewarp-architecture.slnx", ".timewarp/dev.jsonc", ".envrc"];

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
      || StartsWithAny(path, CiRoots)
      || StartsWithAny(path, SkillDevCliPaths)
      || StartsWithAny(path, HostDevCliPaths);
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
  /// Which ci-group legs run beside ci (fast) (see the Design region). Every leg runs unless
  /// <paramref name="pullRequest"/> and <paramref name="changed"/> is known; then a leg runs when a
  /// changed path outside the fast-only roots builds (<paramref name="requiresBuild"/>), or a dev CLI
  /// file that leg runs changed.
  /// </summary>
  internal static CiLegs ClassifyCiLegs(IReadOnlyList<string>? changed, bool pullRequest, Func<IReadOnlyList<string>, bool> requiresBuild)
  {
    ArgumentNullException.ThrowIfNull(requiresBuild);
    if (changed is null || !pullRequest) return CiLegs.All;

    string[] normalized = [.. changed.Select(Normalize)];
    string[] rest = [.. normalized.Where(path => !StartsWithAny(path, FastOnlyRoots))];
    bool full = rest.Length > 0 && requiresBuild(rest);
    bool host = normalized.Any(path => StartsWithAny(path, HostDevCliPaths));
    return new CiLegs(
      Integration: full || host || normalized.Any(path => StartsWithAny(path, IntegrationDevCliPaths)),
      Publish: full || host || normalized.Any(path => StartsWithAny(path, PublishDevCliPaths)));
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

/// <summary>
/// Whether ci (integration) and ci (publish) run beside ci (fast). <see cref="Full"/> is every leg:
/// only such a run may write the ci green-key marker.
/// </summary>
internal readonly record struct CiLegs(bool Integration, bool Publish)
{
  /// <summary>GITHUB_OUTPUT name of the JSON list of legs (the ci-group matrix).</summary>
  internal const string LegsOutputName = "ci_legs";

  /// <summary>GITHUB_OUTPUT name that lets the green-key job upload the ci marker.</summary>
  internal const string MarkerOutputName = "ci_marker";

  internal static CiLegs All { get; } = new(Integration: true, Publish: true);

  internal bool Full => Integration && Publish;

  /// <summary>The leg names in matrix order; fast is always first.</summary>
  internal string[] Names
  {
    get
    {
      List<string> names = [WorkflowPathClasses.CiLegNames[0]];
      if (Integration) names.Add(WorkflowPathClasses.CiLegNames[1]);
      if (Publish) names.Add(WorkflowPathClasses.CiLegNames[2]);
      return [.. names];
    }
  }

  /// <summary>
  /// The ci_legs and ci_marker output lines. <paramref name="ci"/> is the ci output after green-key
  /// hits: the marker needs ci-group to run every leg.
  /// </summary>
  internal string[] OutputLines(bool ci) =>
    [$"{LegsOutputName}=[{string.Join(',', Names.Select(name => $"\"{name}\""))}]", $"{MarkerOutputName}={(ci && Full ? "true" : "false")}"];
}
