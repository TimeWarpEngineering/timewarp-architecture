#region Purpose
// Guards `dev workflow --phase classify`: which changed paths turn on skill lint and template smoke,
// how a push event's before/after SHAs are read, and how a known or unknown diff becomes the ci,
// skill, and template outputs.
#endregion

namespace WorkflowPathClasses_;

using System.Text.Json;

public class IsSkillPath_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsSkillPath_Given_>();

  public static Task SkillAndCiPaths_Should_RunSkillLint()
  {
    string[] paths =
    [
      "skills/tw-deploy/SKILL.md",
      "skills/tw-deploy/references/azure-container-apps.md",
      "source/foo/SKILL.md",
      "skills/tw-blazor/evals/eval.yaml",
      ".vally.yaml",
      "evals/readme.md",
      ".github/workflows/workflow.yml",
      ".github/actions/setup-ci/action.yml",
      "tools/dev-cli/services/skill-spec-lint.cs",
    ];

    foreach (string path in paths)
    {
      WorkflowPathClasses.IsSkillPath(path).ShouldBeTrue(path);
    }

    return Task.CompletedTask;
  }

  public static Task ProductAndKanbanPaths_Should_NotRunSkillLint()
  {
    string[] paths =
    [
      "source/container-apps/web/projects/web-spa/program.cs",
      "kanban/to-do/503-task/task.md",
      "Directory.Packages.props",
      "tools/agent-identity-cli/program.cs",
      "documentation/developer/guides/release.md",
    ];

    foreach (string path in paths)
    {
      WorkflowPathClasses.IsSkillPath(path).ShouldBeFalse(path);
    }

    return Task.CompletedTask;
  }
}

public class IsTemplatePath_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsTemplatePath_Given_>();

  public static Task TemplateContentAndCiPaths_Should_RunTemplateSmoke()
  {
    string[] paths =
    [
      "source/container-apps/web/projects/web-spa/program.cs",
      "tests/common/timewarp-testing/in-proc-test-ports.cs",
      "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj",
      ".template.config/template.json",
      "msbuild/timewarp-platform-packages.props",
      "Directory.Build.props",
      "Directory.Packages.props",
      "timewarp-architecture.slnx",
      ".timewarp/dev.jsonc",
      ".envrc",
      "./tools/dev-cli/endpoints/template-smoke-command.cs",
      ".github/workflows/workflow.yml",
      ".github/actions/setup-ci/action.yml",
      ".github/app-workflows/workflow.yml",
    ];

    foreach (string path in paths)
    {
      WorkflowPathClasses.IsTemplatePath(path).ShouldBeTrue(path);
    }

    return Task.CompletedTask;
  }

  public static Task SkillKanbanAndGoldenPaths_Should_NotRunTemplateSmoke()
  {
    string[] paths =
    [
      "skills/tw-deploy/SKILL.md",
      "kanban/to-do/503-task/task.md",
      "goldens/ingress/WebServerApiRoutePrefixes.g.cs",
      "tools/agent-identity-cli/program.cs",
      "documentation/Directory.Build.props",
    ];

    foreach (string path in paths)
    {
      WorkflowPathClasses.IsTemplatePath(path).ShouldBeFalse(path);
    }

    return Task.CompletedTask;
  }
}

public class TryReadPushShas_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryReadPushShas_Given_>();

  private const string Before = "0123456789abcdef0123456789abcdef01234567";
  private const string After = "89abcdef0123456789abcdef0123456789abcdef";

  public static Task PushEvent_Should_ReadBeforeAndAfter()
  {
    string json = $$"""{ "before": "{{Before}}", "after": "{{After}}", "ref": "refs/heads/master" }""";
    WorkflowPathClasses.TryReadPushShas(json, out string before, out string after).ShouldBeTrue();
    before.ShouldBe(Before);
    after.ShouldBe(After);
    return Task.CompletedTask;
  }

  public static Task NewBranch_Should_HaveNoBase()
  {
    string json = $$"""{ "before": "{{new string('0', 40)}}", "after": "{{After}}" }""";
    WorkflowPathClasses.TryReadPushShas(json, out _, out _).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task MissingOrInvalid_Should_HaveNoBase()
  {
    WorkflowPathClasses.TryReadPushShas(null, out _, out _).ShouldBeFalse();
    WorkflowPathClasses.TryReadPushShas("not json", out _, out _).ShouldBeFalse();
    WorkflowPathClasses.TryReadPushShas($$"""{ "after": "{{After}}" }""", out _, out _).ShouldBeFalse();
    WorkflowPathClasses.TryReadPushShas($$"""{ "before": "zz-not-a-sha", "after": "{{After}}" }""", out _, out _)
      .ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class Classify_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Classify_Given_>();

  private static readonly string[] SkillOnlyPush =
  [
    "skills/tw-deploy/SKILL.md",
    "skills/tw-blazor/evals/eval.yaml",
    "evals/readme.md",
    ".vally.yaml",
  ];

  public static Task UnknownDiff_Should_RunEveryJob()
  {
    WorkflowJobs jobs = WorkflowPathClasses.Classify(
      null,
      _ => throw new InvalidOperationException("An unknown diff must not reach the build rule."));

    jobs.ShouldBe(new WorkflowJobs(Ci: true, Skill: true, Template: true));
    return Task.CompletedTask;
  }

  public static Task SkillOnlyDiff_Should_TakeCiFromTheBuildRule()
  {
    IReadOnlyList<string>? ruleInput = null;
    WorkflowJobs jobs = WorkflowPathClasses.Classify(
      SkillOnlyPush,
      paths =>
      {
        ruleInput = paths;
        return false;
      });

    ruleInput.ShouldBe(SkillOnlyPush);
    jobs.ShouldBe(new WorkflowJobs(Ci: false, Skill: true, Template: false));
    return Task.CompletedTask;
  }

  public static Task BuildDiff_Should_RunCiAndTemplateSmoke()
  {
    string[] changed = ["source/container-apps/web/projects/web-spa/program.cs", "timewarp-architecture.slnx"];
    WorkflowJobs jobs = WorkflowPathClasses.Classify(changed, _ => true);

    jobs.ShouldBe(new WorkflowJobs(Ci: true, Skill: false, Template: true));
    return Task.CompletedTask;
  }

  // The build rule is PullRequestBuildClassifier.RequiresBuild (TimeWarp.Nuru.DevCli content, not
  // compiled into this project). This repo's half of that rule is .timewarp/dev.jsonc: the skill
  // paths on.push.paths starts a run for must be non-build there, and the CI setup action must build.
  public static Task RepoBuildConfig_Should_SkipSkillPathsAndBuildTheSetupAction()
  {
    string path = Path.Combine(RepoRoot(), ".timewarp", "dev.jsonc");
    using var document = JsonDocument.Parse(
      File.ReadAllText(path),
      new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
    JsonElement pullRequestBuild = document.RootElement.GetProperty("pullRequestBuild");

    string[] nonBuild = Entries(pullRequestBuild, "nonBuildPaths");
    nonBuild.ShouldContain("skills/**");
    nonBuild.ShouldContain("evals/**");
    nonBuild.ShouldContain(".vally.yaml");

    string[] build = Entries(pullRequestBuild, "buildPaths");
    build.ShouldContain(".github/actions/**");
    build.ShouldContain("timewarp-templates/**");
    return Task.CompletedTask;
  }

  private static string[] Entries(JsonElement pullRequestBuild, string name) =>
    [.. pullRequestBuild.GetProperty(name).EnumerateArray().Select(entry => entry.GetString() ?? "")];

  private static string RepoRoot()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git")))
    {
      directory = directory.Parent;
    }

    return directory.ShouldNotBeNull().FullName;
  }
}
