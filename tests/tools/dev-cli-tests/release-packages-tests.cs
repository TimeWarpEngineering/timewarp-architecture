#region Purpose
// Guards the template repository's release packages (task 533): the packable set is the 12 packages
// (source/ plus the template pack), which master runs publish Packages-* and how (pack-only, carry
// forward), and the workflow.yml shape that promotes them.
#endregion

namespace ReleasePackages_;

using System.Text.Json;
using TimeWarp.Amuru;

public class CandidateProjectPaths_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CandidateProjectPaths_Given_>();

  public static Task SourceAndTemplate_Should_BeCandidatesWithoutBuildOutput()
  {
    string root = Directory.CreateTempSubdirectory("release-packages-").FullName;
    try
    {
      string library = Touch(root, "source/libraries/a/a.csproj");
      Touch(root, "source/libraries/a/obj/staged.csproj");
      Touch(root, "source/libraries/a/bin/release/copy.csproj");
      string template = Touch(root, ReleasePackages.TemplateProjectRelativePath);
      Touch(root, "timewarp-templates/source/other/other.csproj");

      ReleasePackages.CandidateProjectPaths(root).ShouldBe([library, template], ignoreOrder: true);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  // The real MSBuild evaluation ArchitecturePackableProjectService runs, over this repository.
  public static async Task Repository_Should_PackTheTwelvePackages()
  {
    string root = Repository.Root();
    List<string> packageIds = [];
    await Parallel.ForEachAsync(ReleasePackages.CandidateProjectPaths(root), async (project, ct) =>
    {
      CommandOutput output = await DotNet.Builder()
        .WithArguments("msbuild", project, "-nologo", "-nodeReuse:false", "-getProperty:IsPackable,PackageId")
        .WithNoValidation()
        .CaptureAsync(ct);
      output.ExitCode.ShouldBe(0, project);

      JsonElement properties = JsonElement.Parse(output.Stdout).GetProperty("Properties");
      if (properties.GetProperty("IsPackable").GetString() == "true")
      {
        lock (packageIds) packageIds.Add(properties.GetProperty("PackageId").GetString().ShouldNotBeNull());
      }
    });

    packageIds.ShouldBe(
      [
        "TimeWarp.402",
        "TimeWarp.Architecture",
        "TimeWarp.Architecture.Analyzers",
        "TimeWarp.Architecture.Attributes",
        "TimeWarp.Architecture.Generators",
        "TimeWarp.Foundation.Application",
        "TimeWarp.Foundation.Contracts",
        "TimeWarp.Foundation.Domain",
        "TimeWarp.Foundation.Infrastructure",
        "TimeWarp.Foundation.Server",
        "TimeWarp.Identity",
        "TimeWarp.Modules",
      ],
      ignoreOrder: true);
  }

  private static string Touch(string root, string relativePath)
  {
    string path = Path.GetFullPath(Path.Combine(root, relativePath));
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, "<Project />");
    return path;
  }
}

public class MasterPackages_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<MasterPackages_Given_>();

  public static Task MasterPushAndDispatch_Should_StagePackages()
  {
    ReleasePackages.StagesPackages("push", "refs/heads/master").ShouldBeTrue();
    ReleasePackages.StagesPackages("workflow_dispatch", "refs/heads/master").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task PullRequestsReleasesAndOtherBranches_Should_NotStagePackages()
  {
    ReleasePackages.StagesPackages("pull_request", "refs/pull/12/merge").ShouldBeFalse();
    ReleasePackages.StagesPackages("pull_request", "refs/heads/master").ShouldBeFalse();
    ReleasePackages.StagesPackages("push", "refs/heads/feature/x").ShouldBeFalse();
    ReleasePackages.StagesPackages("release", "refs/tags/v2.0.0").ShouldBeFalse();
    ReleasePackages.StagesPackages("schedule", "refs/heads/master").ShouldBeFalse();
    ReleasePackages.StagesPackages(null, null).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task MasterWithCiOff_Should_RunPackOnly()
  {
    ReleasePackages.OutputLines(stages: true, ciRuns: false, carryRun: null).ShouldBe(["packages=true", "pack=true", "carry_run="]);
    ReleasePackages.OutputLines(stages: true, ciRuns: false, carryRun: 42).ShouldBe(["packages=true", "pack=true", "carry_run=42"]);
    return Task.CompletedTask;
  }

  public static Task MasterWithCiOn_Should_PackInCiGroup()
  {
    ReleasePackages.OutputLines(stages: true, ciRuns: true, carryRun: 42).ShouldBe(["packages=true", "pack=false", "carry_run="]);
    return Task.CompletedTask;
  }

  public static Task PullRequest_Should_StageNothing()
  {
    ReleasePackages.OutputLines(stages: false, ciRuns: false, carryRun: 42).ShouldBe(["packages=false", "pack=false", "carry_run="]);
    return Task.CompletedTask;
  }

  public static Task CarryRun_Should_BeTheNewestLiveNonPullRequestRun()
  {
    CarryCandidate[] candidates =
    [
      new(900, "pull_request", true),
      new(800, "push", true),
      new(850, "push", false),
      new(700, "workflow_dispatch", true),
      new(950, "push", true),
    ];

    ReleasePackages.SelectCarryRun(candidates, currentRunId: 950).ShouldBe(800);
    ReleasePackages.SelectCarryRun([new(900, "pull_request", true)], currentRunId: 1).ShouldBeNull();
    ReleasePackages.SelectCarryRun([], currentRunId: 1).ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class Workflow_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Workflow_Given_>();

  private const string WorkflowPath = ".github/workflows/workflow.yml";

  public static Task Workflow_Should_HaveThePromotionJobsAndInputs()
  {
    List<string> errors = [];
    HashSet<string> keys = AppCiWorkflowShape.ParseKeyPaths(Workflow(), errors);

    errors.ShouldBeEmpty();
    keys.ShouldContain("jobs/pack-only");
    keys.ShouldContain("jobs/release/permissions/actions");
    keys.ShouldContain("jobs/ci/permissions/actions");
    keys.ShouldContain("on/workflow_dispatch/inputs/confirm");
    keys.ShouldContain("jobs/classify/outputs/packages");
    keys.ShouldContain("jobs/classify/outputs/carry_run");
    return Task.CompletedTask;
  }

  public static Task ReleaseJob_Should_PromoteWithoutBuildingOrUploading()
  {
    string release = Job("release");

    release.ShouldContain("fetch-depth: 0");
    release.ShouldContain("actions: read");
    release.ShouldContain("GH_TOKEN: ${{ github.token }}");
    release.ShouldContain("inputs.confirm != 'release'");
    release.ShouldContain("workflow --mode release --api-key");
    release.ShouldContain("aspire-cli: 'true'");
    release.ShouldNotContain("upload-artifact");
    return Task.CompletedTask;
  }

  public static Task PackagesArtifact_Should_BeUploadedOnlyByCiOnMaster()
  {
    string workflow = Workflow();
    Occurrences(workflow, "name: Packages-").ShouldBe(1);

    string ci = Job("ci");
    ci.ShouldContain("name: Packages-${{ github.run_number }}");
    ci.ShouldContain("if: success() && needs.classify.outputs.packages == 'true'");
    ci.ShouldContain("retention-days: 3");
    ci.ShouldContain("$keep = 2");
    ci.ShouldContain("select(.name == \"staged-packages\")");
    ci.ShouldContain("needs: [classify, ci-group, pack-only]");
    return Task.CompletedTask;
  }

  public static Task StagedPackages_Should_ComeFromPublishOrPackOnly()
  {
    Job("ci-group").ShouldContain("if: success() && matrix.name == 'publish' && needs.classify.outputs.packages == 'true'");
    Job("ci-group").ShouldContain("needs.classify.outputs.packages == 'true' && ' --pack'");

    string packOnly = Job("pack-only");
    packOnly.ShouldContain("needs.classify.outputs.pack == 'true'");
    packOnly.ShouldContain("--phase pack --carry-run");
    packOnly.ShouldContain("name: staged-packages");
    // Two uploads (ci (publish), pack-only) and the ci job's download.
    Occurrences(Workflow(), "name: staged-packages").ShouldBe(3);
    Occurrences(Job("ci"), "name: staged-packages").ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task PackOnly_Should_WriteNoGreenKeyMarker()
  {
    string greenKey = Job("green-key");
    greenKey.ShouldContain("needs: [classify, ci-group, skill-lint, template-smoke]");
    greenKey.ShouldNotContain("pack-only");
    Job("pack-only").ShouldNotContain("green-key-v1");
    return Task.CompletedTask;
  }

  private static string Workflow() => File.ReadAllText(Path.Combine(Repository.Root(), WorkflowPath));

  // The job's lines: from its two-space key under jobs: to the next two-space key.
  private static string Job(string name)
  {
    string[] lines = Workflow().Split('\n');
    int jobs = Array.IndexOf(lines, "jobs:");
    int from = Array.IndexOf(lines, $"  {name}:", jobs + 1);
    from.ShouldBeGreaterThan(jobs, name);

    int to = from + 1;
    while (to < lines.Length && !IsJobKey(lines[to])) to++;
    return string.Join('\n', lines[from..to]);
  }

  private static bool IsJobKey(string line) =>
    line.Length > 3 && line.StartsWith("  ", StringComparison.Ordinal) && line[2] != ' ' && line[2] != '#' && line[^1] == ':';

  private static int Occurrences(string text, string value) =>
    (text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;
}

internal static class Repository
{
  internal static string Root()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git")))
    {
      directory = directory.Parent;
    }

    return directory.ShouldNotBeNull().FullName;
  }
}
