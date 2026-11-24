#region Purpose
// Guards the green-key ledger rules behind `dev workflow --phase classify`: which tracked paths the key
// hashes, that the key is stable and changes with its inputs, which events may skip, the force
// trailer, marker selection, and which jobs a hit turns off.
#endregion

namespace GreenKey_;

public class IsKeyInputPath_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsKeyInputPath_Given_>();

  public static Task CodeAndPackedKanbanPaths_Should_BeKeyInputs()
  {
    string[] paths =
    [
      "source/container-apps/web/projects/web-spa/program.cs",
      ".github/workflows/workflow.yml",
      "global.json",
      "documentation/developer/client-names.txt",
      "skills/tw-deploy/SKILL.md",
      "kanban/overview.md",
      "kanban/task-template.md",
      "kanban/to-do/.gitkeep",
      "kanban/archived/.gitkeep",
      "./tools/dev-cli/dev.cs",
    ];

    foreach (string path in paths)
    {
      GreenKey.IsKeyInputPath(path).ShouldBeTrue(path);
    }

    return Task.CompletedTask;
  }

  public static Task KanbanTaskContent_Should_NotBeKeyInputs()
  {
    string[] paths =
    [
      "kanban/to-do/503-task/task.md",
      "kanban/done/504-move-ci/task.md",
      "kanban/in-progress/504-move-ci/review/round-1/findings.md",
      "kanban/backlog/600-idea.md",
      "kanban/task-examples/example.md",
      "",
    ];

    foreach (string path in paths)
    {
      GreenKey.IsKeyInputPath(path).ShouldBeFalse(path);
    }

    return Task.CompletedTask;
  }
}

public class Manifest_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Manifest_Given_>();

  private const string LsTree =
    "100644 blob 1111111111111111111111111111111111111111\tsource/b.cs\n" +
    "100644 blob 2222222222222222222222222222222222222222\tkanban/done/504-x/task.md\n" +
    "160000 commit 3333333333333333333333333333333333333333\tvendor/sub\n" +
    "100644 blob 4444444444444444444444444444444444444444\tsource/a.cs\n" +
    "040000 tree 5555555555555555555555555555555555555555\tsource\n" +
    "malformed line\n";

  public static Task LsTree_Should_KeepBlobsAndSubmodulesMinusKanbanTasks()
  {
    IReadOnlyList<GreenKeyEntry> entries = GreenKey.ParseLsTree(LsTree);

    entries.Select(entry => entry.Path).ShouldBe(["source/b.cs", "vendor/sub", "source/a.cs"]);
    return Task.CompletedTask;
  }

  public static Task Manifest_Should_SortTreeLinesAndHashToAKey()
  {
    IReadOnlyList<GreenKeyEntry> entries = GreenKey.ParseLsTree(LsTree);
    string manifest = GreenKey.Manifest("11.0.100", "self-hosted|dpkg:ab", entries);

    manifest.ShouldBe(
      "green-key-v1\n" +
      "sdk 11.0.100\n" +
      "runner self-hosted|dpkg:ab\n" +
      "tree source/a.cs 4444444444444444444444444444444444444444\n" +
      "tree source/b.cs 1111111111111111111111111111111111111111\n" +
      "tree vendor/sub 3333333333333333333333333333333333333333\n");
    GreenKey.IsKey(GreenKey.Hash(manifest)).ShouldBeTrue();
    GreenKey.Hash(manifest).ShouldBe(GreenKey.Hash(GreenKey.Manifest("11.0.100", "self-hosted|dpkg:ab", entries.Reverse())));
    return Task.CompletedTask;
  }

  public static Task ChangedInput_Should_ChangeTheKey_And_KanbanTaskEdit_Should_Not()
  {
    string baseline = GreenKey.Hash(GreenKey.Manifest("11.0.100", "r", GreenKey.ParseLsTree(LsTree)));
    string kanbanEdit = LsTree.Replace("2222222222222222222222222222222222222222", "9999999999999999999999999999999999999999", StringComparison.Ordinal);
    string codeEdit = LsTree.Replace("1111111111111111111111111111111111111111", "9999999999999999999999999999999999999999", StringComparison.Ordinal);

    GreenKey.Hash(GreenKey.Manifest("11.0.100", "r", GreenKey.ParseLsTree(kanbanEdit))).ShouldBe(baseline);
    GreenKey.Hash(GreenKey.Manifest("11.0.100", "r", GreenKey.ParseLsTree(codeEdit))).ShouldNotBe(baseline);
    GreenKey.Hash(GreenKey.Manifest("11.0.101", "r", GreenKey.ParseLsTree(LsTree))).ShouldNotBe(baseline);
    GreenKey.Hash(GreenKey.Manifest("11.0.100", "other", GreenKey.ParseLsTree(LsTree))).ShouldNotBe(baseline);
    return Task.CompletedTask;
  }

  public static Task Runner_Should_DescribeHostedAndSelfHostedRunners()
  {
    GreenKey.RunnerDescription(null, "github-hosted", "ubuntu24", "20261116.1", null)
      .ShouldBe("github-hosted|image:ubuntu24:20261116.1");
    GreenKey.RunnerDescription("", "self-hosted", null, null, "ABCDEF")
      .ShouldBe("self-hosted|dpkg:abcdef");
    GreenKey.RunnerDescription("twe-runner-01@sha256:1", "self-hosted", null, null, "abc")
      .ShouldBe("twe-runner-01@sha256:1");
    return Task.CompletedTask;
  }

  public static Task ArtifactName_Should_NameGateAndLowercaseKey()
  {
    string key = new('A', 64);
    GreenKey.ArtifactName(GreenKey.GateCi, key).ShouldBe("green-key-v1-ci-" + new string('a', 64));
    GreenKey.IsKey("abc").ShouldBeFalse();
    GreenKey.IsKey(new string('g', 64)).ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class ClassifyEvent_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ClassifyEvent_Given_>();

  private const string Repo = "TimeWarpEngineering/timewarp-architecture";

  public static Task PushAndSameRepoPullRequest_Should_BeCandidates()
  {
    GreenKey.ClassifyEvent("push", Repo, null).Candidate.ShouldBeTrue();
    GreenKey.ClassifyEvent("pull_request", Repo, Repo).Candidate.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task ForkAndOtherEvents_Should_NotBeCandidates()
  {
    GreenKey.ClassifyEvent("pull_request", Repo, "someone/timewarp-architecture").ShouldBe(new GreenKeyEventClass(false, "fork-pull-request"));
    GreenKey.ClassifyEvent("pull_request", Repo, null).Candidate.ShouldBeFalse();
    GreenKey.ClassifyEvent("workflow_dispatch", Repo, null).ShouldBe(new GreenKeyEventClass(false, "workflow_dispatch"));
    GreenKey.ClassifyEvent("release", Repo, null).Candidate.ShouldBeFalse();
    GreenKey.ClassifyEvent("schedule", Repo, null).Candidate.ShouldBeFalse();
    GreenKey.ClassifyEvent(null, Repo, null).ShouldBe(new GreenKeyEventClass(false, "local"));
    return Task.CompletedTask;
  }

  public static Task PullRequestEvent_Should_ReadHeadShaAndRepo()
  {
    const string json = """{ "pull_request": { "head": { "sha": "abc123", "repo": { "full_name": "o/r" } } } }""";

    GreenKey.ReadPullRequestHead(json, "sha").ShouldBe("abc123");
    GreenKey.ReadPullRequestHead(json, "repo").ShouldBe("o/r");
    GreenKey.ReadPullRequestHead("""{ "after": "abc" }""", "sha").ShouldBeNull();
    GreenKey.ReadPullRequestHead("not json", "sha").ShouldBeNull();
    GreenKey.ReadPullRequestHead(null, "repo").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task ForceTrailer_Should_MatchFullRunIgnoringCase()
  {
    GreenKey.IsForceFullRun("full-run\n").ShouldBeTrue();
    GreenKey.IsForceFullRun("other\nFULL-RUN").ShouldBeTrue();
    GreenKey.IsForceFullRun("").ShouldBeFalse();
    GreenKey.IsForceFullRun("skip\n").ShouldBeFalse();
    GreenKey.IsForceFullRun(null).ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class SelectMarker_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SelectMarker_Given_>();

  public static Task Rows_Should_PickTheEarliestLiveMarker()
  {
    string[] rows =
    [
      """{"id":3,"expired":false,"created_at":"2026-11-20T10:00:00Z","run_id":30,"head_sha":"c3"}""",
      """{"id":1,"expired":true,"created_at":"2026-11-01T10:00:00Z","run_id":10,"head_sha":"c1"}""",
      """{"id":2,"expired":false,"created_at":"2026-11-10T10:00:00Z","run_id":20,"head_sha":"c2"}""",
      "not json",
      "",
    ];

    GreenKeyMarker marker = GreenKey.SelectMarker(rows).ShouldNotBeNull();
    marker.ArtifactId.ShouldBe(2);
    marker.RunId.ShouldBe(20);
    marker.Commit.ShouldBe("c2");
    return Task.CompletedTask;
  }

  public static Task NoLiveRow_Should_BeAMiss()
  {
    GreenKey.SelectMarker(["""{"id":1,"expired":true,"run_id":10}""", ""]).ShouldBeNull();
    GreenKey.SelectMarker([]).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Hits_Should_TurnOffOnlyTheirGates()
  {
    WorkflowJobs jobs = new(Ci: true, Skill: true, Template: false);

    GreenKey.ApplyHits(jobs, [GreenKey.GateCi]).ShouldBe(new WorkflowJobs(false, true, false));
    GreenKey.ApplyHits(jobs, [GreenKey.GateTemplate]).ShouldBe(jobs);
    GreenKey.ApplyHits(jobs, []).ShouldBe(jobs);
    return Task.CompletedTask;
  }
}
