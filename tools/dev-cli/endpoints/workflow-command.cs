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
// workflow.yml's break-glass dispatch (mode release, confirm release) passes --mode release.
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
// Green key (services/green-key.cs, services/green-key-ledger.cs): classify turns ci off when the ci
// gate already passed on this tree, and writes green_key and green_key_writer for the workflow's
// green-key job, which uploads the marker after ci-group passes.
//
// Template repository and generated app (task 530). The template pack ships this file to every
// generated app, with that app's workflow.yml (task 531; its source here is
// .github/app-workflows/workflow.yml, which .template.config/template.json renames into place). These
// paths are the template repository's own and live in files template.json excludes from generated
// apps; the call sites here are templateRepository regions:
//   - Release mode (WorkflowTemplateRelease, task 533) promotes the master run's tested Packages-*:
//     tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push, then
//     template-publish-smoke. A generated app publishes no packages, so its release mode refuses
//     and names the deploy command. Its workflow's deploy job runs that command on a published
//     release (task 529); no `dev workflow` mode deploys.
//   - Classify (WorkflowTemplateRepository) also writes the skill and template outputs, from the
//     changed-path rules in services/workflow-path-classes.cs, the master packages outputs, and
//     verifies attestation. A generated app's classify writes ci and the green key only.
//   - Whenever the test step includes the fast group, the regenerated feature filename grammar and
//     ingress golden must show no diff.
//   - Master packages (WorkflowTemplatePackages, task 533): --pack on the publish phase packs and
//     verifies the 12-package set after publish all; --phase pack is workflow.yml's pack-only job
//     (--carry-run <id> republishes that run's Packages-*, else clean -> build -> pack).
// --api-key, --attestation, --pack, and --carry-run are templateRepository regions too.
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

  [Option("attestation", Description = "Attestation mode for this run: off, warn, or require (default: dev.jsonc attestation.mode, else warn on pr/merge and require on release)")]
  public string? Attestation { get; set; }

  [Option("pack", Description = "With --phase publish: pack the packable set into artifacts/packages and verify it (master runs)")]
  public bool Pack { get; set; }

  [Option("carry-run", Description = "With --phase pack: republish this run's live Packages-* instead of packing")]
  public string? CarryRun { get; set; }
#endif

#if templateRepository
  [Option("phase", "p", Description = "Pr/merge only: run one phase, classify, test, publish, or pack (default: all but pack)")]
#else
  [Option("phase", "p", Description = "Pr/merge only: run one phase, classify, test, or publish (default: all)")]
#endif
  public string? Phase { get; set; }

  [Option("group", "g", Description = "With --phase test: run one test group, fast, playwright, or integration (default: all)")]
  public string? Group { get; set; }

  internal sealed class Handler : IRequestHandler<WorkflowCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly IRepoCleanService RepoCleanService;
#if templateRepository
    private readonly IPackableProjectService PackableProjectService;
    private readonly NuGetVersionService NuGetVersionService;
    private readonly IRepoConfigService ConfigService;
#endif
    private CancellationToken Ct;
    private string RepoRoot = null!;

#if templateRepository
    public Handler(
      ITerminal terminal,
      IRepoCleanService repoCleanService,
      IPackableProjectService packableProjectService,
      NuGetVersionService nuGetVersionService,
      IRepoConfigService configService)
    {
      Terminal = terminal;
      RepoCleanService = repoCleanService;
      PackableProjectService = packableProjectService;
      NuGetVersionService = nuGetVersionService;
      ConfigService = configService;
    }

    private WorkflowTemplatePackages Packages => new(Terminal, RepoCleanService, PackableProjectService, RepoRoot, Ct);
#else
    public Handler(ITerminal terminal, IRepoCleanService repoCleanService)
    {
      Terminal = terminal;
      RepoCleanService = repoCleanService;
    }
#endif

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
        await new WorkflowTemplateRelease(Terminal, ConfigService, NuGetVersionService, PackableProjectService, Packages, RepoRoot, Ct)
          .RunAsync(command.ApiKey, command.Attestation);
#else
        Terminal.WriteErrorLine("Error: release mode packs and pushes the template's NuGet packages. This app publishes no packages: its workflow's deploy job, or an operator, deploys it with this CLI's deploy command.".Red());
        Environment.ExitCode = 1;
#endif
        return Unit.Value;
      }

      string phase = command.Phase ?? PhaseAll;
#if templateRepository
      if (phase == PhasePack)
      {
        await Packages.RunPackOnlyAsync(command.CarryRun is null ? null : long.Parse(command.CarryRun, System.Globalization.CultureInfo.InvariantCulture));
        return Unit.Value;
      }

#endif
      if (phase is PhaseAll or PhaseClassify)
      {
        PullRequestBuildDecision decision = await PullRequestBuildClassifier.DecideAsync(RepoRoot, ct);
        Terminal.WriteLine(decision.Message);
        if (phase == PhaseClassify)
        {
#if templateRepository
          await new WorkflowTemplateRepository(Terminal, RepoRoot, Ct).ClassifyAsync(decision, command.Attestation);
#else
          var pathJobs = new WorkflowJobs(Ci: decision.RequiresBuild, Skill: false, Template: false);
          GreenKeyObservation green = await GreenKeyLedger.ObserveAsync(Terminal, RepoRoot, pathJobs, Ct);
          WorkflowJobs jobs = GreenKey.ApplyHits(pathJobs, green.HitGates);
          await WriteOutputsAsync(Terminal, [$"ci={Bool(jobs.Ci)}", .. GreenKey.OutputLines(green, jobs)], Ct);
#endif
          return Unit.Value;
        }

        if (!decision.RequiresBuild) return Unit.Value;
      }

#if templateRepository
      await RunPrAsync(phase, command.Group, command.Pack);
#else
      await RunPrAsync(phase, command.Group);
#endif
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
#if templateRepository
    private const string PhasePack = "pack";
#endif

    private bool ValidatePhase(WorkflowCommand command, CiMode mode)
    {
      string? error = null;
      if (mode == CiMode.Release && (command.Phase is not null || command.Group is not null))
        error = "--phase and --group apply to pr/merge only; release runs as one job.";
#if templateRepository
      else if (command.Phase is not null && command.Phase is not (PhaseClassify or PhaseTest or PhasePublish or PhasePack))
        error = $"Unknown phase '{command.Phase}'. Phases: {PhaseClassify}, {PhaseTest}, {PhasePublish}, {PhasePack}.";
      else if (command.Pack && command.Phase != PhasePublish)
        error = "--pack requires --phase publish.";
      else if (command.CarryRun is not null && command.Phase != PhasePack)
        error = "--carry-run requires --phase pack.";
      else if (command.CarryRun is not null && !long.TryParse(command.CarryRun, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _))
        error = $"--carry-run takes a workflow run id, not '{command.CarryRun}'.";
#else
      else if (command.Phase is not null && command.Phase is not (PhaseClassify or PhaseTest or PhasePublish))
        error = $"Unknown phase '{command.Phase}'. Phases: {PhaseClassify}, {PhaseTest}, {PhasePublish}.";
#endif
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
#if templateRepository
    private async Task RunPrAsync(string phase, string? group, bool pack)
#else
    private async Task RunPrAsync(string phase, string? group)
#endif
    {
      bool test = phase is PhaseAll or PhaseTest;
      bool publish = phase is PhaseAll or PhasePublish;
#if templateRepository
      string publishSteps = pack ? "publish all -> pack" : "publish all";
#else
      const string publishSteps = "publish all";
#endif
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
      if (golden && !await new WorkflowTemplateRepository(Terminal, RepoRoot, Ct).VerifyGeneratedFilesAsync()) return;
#endif
      if (publish && !await RunStepAsync("Publish all", new PublishAllCommand.Handler(Terminal).Handle(new PublishAllCommand { NoBuild = true }, Ct))) return;
#if templateRepository
      if (pack && !await Packages.PackAsync()) return;
#endif

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
