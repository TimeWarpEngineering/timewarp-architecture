#region Purpose
// Guards `dev workflow --phase classify`: which changed paths turn on skill lint and template smoke,
// and how a push event's before/after SHAs are read.
#endregion

namespace WorkflowPathClasses_;

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
      "./tools/dev-cli/endpoints/template-smoke-command.cs",
      ".github/workflows/workflow.yml",
      ".github/actions/setup-ci/action.yml",
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
