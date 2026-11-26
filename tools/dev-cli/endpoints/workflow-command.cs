#region Purpose
// Full CI/CD pipeline, mode-aware (mirrors the TimeWarp.Nuru / TimeWarp.Amuru pattern).
#endregion
#region Design
// workflow.yml is a thin trigger: it says when to run and on which runner, and calls this command.
// Runner setup (.NET from global.json, the NuGet cache, dev-cert trust, the pinned Aspire CLI) is the
// one composite action .github/actions/setup-ci. Every decision and check lives here.
//
// Mode is auto-detected from GITHUB_EVENT_NAME (or forced with --mode):
//   pull_request / push  -> Pr/Merge:  clean -> build -> test -> publish all (tests are the gate)
//   release / dispatch   -> Release:   the template repository's NuGet release (below)
// A dispatch that names a --phase is Merge: workflow.yml's dispatch mode full runs the CI jobs.
// Pr/Merge splits into phases so workflow.yml runs them as parallel jobs (ci (fast), ci
// (integration), ci (publish)), each on its own runner that builds what it needs: a shared build
// artifact would be bin/obj for the whole solution, far over the org's 500 MB Actions storage cap.
// --phase test [--group <name>] is clean -> build -> test (one TestGroups group, or all); --phase
// publish is clean -> build -> publish all. No --phase runs everything in order. Release mode
// accepts no --phase/--group. The playwright group is not in the pull request matrix (task 501).
// It runs nightly through `template-smoke --matrix --app-tests` (task 502); locally
// `dev test --group playwright` or `dev test <file>` runs it.
// The test step runs `dev test --no-build`, and publish runs `dev publish all --no-build` (every
// target, their safety suites in one aspire-tests run, then helm lint), against the Release build
// that just ran. Publish outputs are files on the runner; CI uploads none of them and deploys
// nothing (deploying is operator-run).
//
// --phase classify is the whole of workflow.yml's classify job. It writes ci to GITHUB_OUTPUT:
// PullRequestBuildClassifier.RequiresBuild (TimeWarp.Nuru.DevCli) with this repo's additions in
// .timewarp/dev.jsonc pullRequestBuild. The test and publish phases do not classify: their shallow
// checkout cannot diff, and the classify job already gated them.
//
// Template repository and generated app (task 530). The template pack ships this file to every
// generated app. Three paths are the template repository's own and live in
// WorkflowTemplateRepository (endpoints/workflow-template-repository.cs), which template.json
// excludes from generated apps; the call sites here are templateRepository regions:
//   - Release mode packs and pushes the platform packages and the template, then runs
//     template-publish-smoke. A generated app publishes no packages, so its release mode refuses
//     and names the operator-run deploy command.
//   - Classify also writes the skill and template outputs and the green key.
//   - Whenever the test step includes the fast group, the regenerated feature filename grammar and
//     ingress golden must show no diff.
// --api-key is a templateRepository region too: only the NuGet push reads it.
//
// Checkouts are shallow everywhere but classify on a pull request: SourceLink and
// TimeWarp.Build.Tasks read only HEAD. Handlers are invoked directly (no ./bin/dev dependency) so
// this runs in a clean CI checkout.
#endregion

namespace DevCli.Commands;

[NuruRoute("workflow", Description = "Execute full CI/CD pipeline (mode-aware)")]
internal sealed class WorkflowCommand : ICommand<Unit>
{
  [Option("mode", "m", Description = "CI mode: pr, merge, or release (auto-detected from GITHUB_EVENT_NAME)")]
  public string? Mode { get; set; }
#if templateRepository

  [Option("api-key", "k", Description = "NuGet API key for publishing (from OIDC Trusted Publishing)")]
  public string? ApiKey { get; set; }
#endif

  [Option("phase", "p", Description = "Pr/merge only: run one phase, classify, test, or publish (default: all)")]
  public string? Phase { get; set; }

  [Option("group", "g", Description = "With --phase test: run one test group, fast, playwright, or integration (default: all)")]
  public string? Group { get; set; }

  internal sealed class Handler : IRequestHandler<WorkflowCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly IRepoCleanService RepoCleanService;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal, IRepoCleanService repoCleanService)
    {
      Terminal = terminal;
      RepoCleanService = repoCleanService;
    }

    public async ValueTask<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Unit.Value;

      CiMode mode = DetermineMode(command.Mode, command.Phase);
      Terminal.WriteLine($"\nCI/CD Pipeline — Mode: {mode}\n".Cyan());

      if (!ValidatePhase(command, mode)) return Unit.Value;

      if (mode == CiMode.Release)
      {
#if templateRepository
        await new WorkflowTemplateRepository(Terminal, RepoCleanService, RepoRoot, Ct).RunReleaseAsync(command.ApiKey);
#else
        Terminal.WriteErrorLine("Error: release mode packs and pushes the template's NuGet packages. This app publishes no packages: an operator deploys it with this CLI's deploy command.".Red());
        Environment.ExitCode = 1;
#endif
        return Unit.Value;
      }

      string phase = command.Phase ?? PhaseAll;
      if (phase is PhaseAll or PhaseClassify)
      {
        PullRequestBuildDecision decision = await PullRequestBuildClassifier.DecideAsync(RepoRoot, ct);
        Terminal.WriteLine(decision.Message);
        if (phase == PhaseClassify)
        {
#if templateRepository
          await new WorkflowTemplateRepository(Terminal, RepoCleanService, RepoRoot, Ct).ClassifyAsync(decision);
#else
          await WriteOutputsAsync(Terminal, [$"ci={Bool(decision.RequiresBuild)}"], Ct);
#endif
          return Unit.Value;
        }

        if (!decision.RequiresBuild) return Unit.Value;
      }

      await RunPrAsync(phase, command.Group);
      return Unit.Value;
    }

    private bool FindRepoRoot()
    {
      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return false;
      }

      RepoRoot = root;
      return true;
    }

    private const string PhaseAll = "all";
    private const string PhaseClassify = "classify";
    private const string PhaseTest = "test";
    private const string PhasePublish = "publish";

    private bool ValidatePhase(WorkflowCommand command, CiMode mode)
    {
      string? error = null;
      if (mode == CiMode.Release && (command.Phase is not null || command.Group is not null))
        error = "--phase and --group apply to pr/merge only; release runs as one job.";
      else if (command.Phase is not null && command.Phase is not (PhaseClassify or PhaseTest or PhasePublish))
        error = $"Unknown phase '{command.Phase}'. Phases: {PhaseClassify}, {PhaseTest}, {PhasePublish}.";
      else if (command.Group is not null && command.Phase != PhaseTest)
        error = "--group requires --phase test.";
      else if (command.Group is not null && !TestGroups.IsGroup(command.Group))
        error = $"Unknown test group '{command.Group}'. Groups: {string.Join(", ", TestGroups.Names)}.";

      if (error is null) return true;

      Terminal.WriteErrorLine($"Error: {error}".Red());
      Environment.ExitCode = 1;
      return false;
    }

    private CiMode DetermineMode(string? explicitMode, string? phase)
    {
      if (!string.IsNullOrEmpty(explicitMode))
      {
        return explicitMode.ToLowerInvariant() switch
        {
          "merge" => CiMode.Merge,
          "release" => CiMode.Release,
          _ => CiMode.Pr,
        };
      }

      string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
      Terminal.WriteLine($"GITHUB_EVENT_NAME: {eventName ?? "(not set)"}");

      return eventName switch
      {
        "push" => CiMode.Merge,
        "release" => CiMode.Release,
        "workflow_dispatch" when phase is not null => CiMode.Merge,
        "workflow_dispatch" => CiMode.Release,
        _ => CiMode.Pr,
      };
    }

    // PR / merge: tests gate here.
    private async Task RunPrAsync(string phase, string? group)
    {
      bool test = phase is PhaseAll or PhaseTest;
      bool publish = phase is PhaseAll or PhasePublish;
      const string publishSteps = "publish all";
      string testSteps = group is null ? "test" : $"test ({group})";
#if templateRepository
      bool golden = test && (group is null or TestGroups.Fast);
      if (golden) testSteps += " -> generated files";
#endif
      string steps = phase switch
      {
        PhaseTest => testSteps,
        PhasePublish => publishSteps,
        _ => $"{testSteps} -> {publishSteps}",
      };
      Terminal.WriteLine($"Pipeline: clean -> build -> {steps}\n");
      Environment.ExitCode = 0;

      if (!await RunStepAsync("Clean", new CleanCommand.Handler(Terminal, RepoCleanService).Handle(new CleanCommand(), Ct))) return;
      if (!await RunStepAsync("Build", new BuildCommand.Handler(Terminal).Handle(new BuildCommand(), Ct))) return;
      if (test && !await RunStepAsync("Test", new TestCommand.Handler(Terminal).Handle(new TestCommand { NoBuild = true, Group = group }, Ct))) return;
#if templateRepository
      if (golden && !await new WorkflowTemplateRepository(Terminal, RepoCleanService, RepoRoot, Ct).VerifyGeneratedFilesAsync()) return;
#endif
      if (publish && !await RunStepAsync("Publish all", new PublishAllCommand.Handler(Terminal).Handle(new PublishAllCommand { NoBuild = true }, Ct))) return;

      Terminal.WriteLine("\nPipeline SUCCEEDED".Green());
    }

    private Task<bool> RunStepAsync(string stepName, ValueTask<Unit> step) =>
      WorkflowCommand.RunStepAsync(Terminal, stepName, step);
  }

  /// <summary>Prints each line and appends it to GITHUB_OUTPUT when that is set.</summary>
  internal static async Task WriteOutputsAsync(ITerminal terminal, string[] outputs, CancellationToken ct)
  {
    string? outputPath = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
    foreach (string line in outputs)
    {
      terminal.WriteLine(line);
      if (!string.IsNullOrEmpty(outputPath)) await File.AppendAllTextAsync(outputPath, line + "\n", ct);
    }
  }

  internal static string Bool(bool value) => value ? "true" : "false";

  /// <summary>Awaits one pipeline step; false (with the failure line) when it left a non-zero exit code.</summary>
  internal static async Task<bool> RunStepAsync(ITerminal terminal, string stepName, ValueTask<Unit> step)
  {
    await step;

    if (Environment.ExitCode != 0)
    {
      terminal.WriteErrorLine($"\nPipeline FAILED — {stepName} failed".Red());
      return false;
    }

    return true;
  }
}

internal enum CiMode
{
  Pr,
  Merge,
  Release,
}
