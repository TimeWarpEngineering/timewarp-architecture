#region Purpose
// Pins the generated app's CI workflow: its source passes the shape check, the template repository's
// own workflow does not, the block-YAML reader parses both workflows and setup-ci, and it reports the
// errors GitHub rejects. template.json and the pack agree on where the workflow comes from and goes.
#endregion

namespace AppCiWorkflowShape_;

using System.Text.Json;

public class Source_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Source_Given_>();

  public static Task AppWorkflow_Should_PassTheShapeCheck()
  {
    AppCiWorkflowShape.Check(Read(AppCiWorkflowShape.SourceRelativePath)).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task TemplateRepositoryWorkflow_Should_FailAsTemplateOnly()
  {
    IReadOnlyList<string> errors = AppCiWorkflowShape.Check(Read(".github/workflows/workflow.yml"));

    errors.ShouldContain("Has the template repository's release job.");
    errors.ShouldContain("Has the template repository's template-smoke job.");
    errors.ShouldContain("Contains template-repository text 'wslc'.");
    return Task.CompletedTask;
  }

  public static Task EveryWorkflowFile_Should_Parse()
  {
    foreach (string path in (string[])[AppCiWorkflowShape.SourceRelativePath, ".github/workflows/workflow.yml", AspireVersionSources.SetupActionRelativePath])
    {
      List<string> errors = [];
      HashSet<string> keys = AppCiWorkflowShape.ParseKeyPaths(Read(path), errors);
      errors.ShouldBeEmpty(path);
      keys.ShouldNotBeEmpty(path);
    }

    return Task.CompletedTask;
  }

  public static Task TemplateJsonAndPack_Should_MapTheSourceIntoPlace()
  {
    using var document = JsonDocument.Parse(Read(".template.config/template.json"));
    JsonElement unconditional = document.RootElement.GetProperty("sources").EnumerateArray()
      .SelectMany(source => source.GetProperty("modifiers").EnumerateArray())
      .Single(modifier => modifier.GetProperty("condition").GetString() == "(true)");

    unconditional.GetProperty("rename").GetProperty(AppCiWorkflowShape.SourceRelativePath).GetString()
      .ShouldBe(AppCiWorkflowShape.AppRelativePath);
    unconditional.GetProperty("exclude").EnumerateArray().Select(entry => entry.GetString())
      .ShouldContain(".github/workflows/**");

    string pack = Read("timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj");
    pack.ShouldContain(@"Include=""..\..\..\.github\app-workflows\workflow.yml""");
    pack.ShouldContain(@"Include=""..\..\..\.github\actions\setup-ci\**\*""");
    pack.ShouldNotContain(@"..\..\..\.github\workflows");
    return Task.CompletedTask;
  }

  internal static string Read(string relativePath)
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git")))
    {
      directory = directory.Parent;
    }

    return File.ReadAllText(Path.Combine(directory.ShouldNotBeNull().FullName, relativePath));
  }
}

public class Check_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Check_Given_>();

  public static Task PullRequestPathsFilter_Should_Fail()
  {
    string yaml = Source().Replace(
      "  pull_request:\n    types: [opened, synchronize, reopened]\n",
      "  pull_request:\n    types: [opened, synchronize, reopened]\n    paths:\n      - 'source/**'\n",
      StringComparison.Ordinal);

    AppCiWorkflowShape.Check(yaml).ShouldContain("on.pull_request has a paths filter: every pull request must report ci.");
    return Task.CompletedTask;
  }

  public static Task RenamedCiCheck_Should_Fail()
  {
    string yaml = Source().Replace("  ci:\n", "  ci:\n    name: Required\n", StringComparison.Ordinal);

    AppCiWorkflowShape.Check(yaml).ShouldContain("Job ci sets a name: the required check must be named ci.");
    return Task.CompletedTask;
  }

  public static Task MissingPublishPhase_Should_Fail()
  {
    string yaml = Source().Replace("args: --phase publish", "args: --phase test", StringComparison.Ordinal);

    AppCiWorkflowShape.Check(yaml).ShouldContain("Does not run `dev workflow --phase publish`.");
    return Task.CompletedTask;
  }

  private static string Source() => Source_Given_.Read(AppCiWorkflowShape.SourceRelativePath);
}

public class ParseKeyPaths_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ParseKeyPaths_Given_>();

  public static Task BlockYaml_Should_YieldKeyPaths()
  {
    const string yaml = """
      on:
        push:
          branches:
            - master
        workflow_dispatch:
      jobs:
        ci:
          runs-on: >-
            ${{ vars.X != '' &&
            'ubuntu-latest' }}
          steps:
            - uses: actions/checkout@v7  # pinned
              with:
                fetch-depth: 1
            - name: Run
              run: |
                echo "a: b"
      """;

    List<string> errors = [];
    HashSet<string> keys = AppCiWorkflowShape.ParseKeyPaths(yaml, errors);

    errors.ShouldBeEmpty();
    keys.ShouldBe(
      [
        "on", "on/push", "on/push/branches", "on/workflow_dispatch",
        "jobs", "jobs/ci", "jobs/ci/runs-on", "jobs/ci/steps",
        "jobs/ci/steps/-/uses", "jobs/ci/steps/-/with", "jobs/ci/steps/-/with/fetch-depth",
        "jobs/ci/steps/-/name", "jobs/ci/steps/-/run",
      ],
      ignoreOrder: true);
    return Task.CompletedTask;
  }

  public static Task Errors_Should_NameTheLine()
  {
    Parse("on:\n   push:\n").ShouldBe(["Line 2: indentation is not a multiple of two spaces."]);
    Parse("on:\n  push:\n  push:\n").ShouldBe(["Line 3: duplicate key 'push'."]);
    Parse("jobs:\n    ci:\n  steps:\n").ShouldBe(["Line 3: indentation does not match an open mapping or sequence."]);
    Parse("name: 'CI\n").ShouldBe(["Line 1: a quoted or flow value does not close on its line."]);
    Parse("on:\n  - push\n  pull_request:\n").ShouldBe(["Line 3: a mapping key where a sequence item belongs."]);
    Parse("on: [push\n").ShouldBe(["Line 1: a quoted or flow value does not close on its line."]);
    Parse("just text\n").ShouldBe(["Line 1: not a key, a sequence item, or a comment."]);
    Parse("on:\n\tpush:\n").ShouldBe(["Contains a tab: YAML indentation is spaces."]);
    return Task.CompletedTask;
  }

  private static List<string> Parse(string yaml)
  {
    List<string> errors = [];
    AppCiWorkflowShape.ParseKeyPaths(yaml, errors);
    return errors;
  }
}
