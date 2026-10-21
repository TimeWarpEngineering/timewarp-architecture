#region Purpose
// Full CI/CD pipeline, mode-aware (mirrors the TimeWarp.Nuru / TimeWarp.Amuru pattern).
#endregion
#region Design
// Mode is auto-detected from GITHUB_EVENT_NAME (or forced with --mode):
//   pull_request / push  -> Pr/Merge:  clean -> build -> test -> publish all (tests are the gate)
//   release / dispatch   -> Release:   clean -> build -> pack -> push -> template-publish-smoke
// publish compose (task 070-003) runs `aspire publish` for the Docker Compose target and gates the
// output with aspire-tests' production-safety suite; publish kubernetes (task 070-004) does the same
// for the Helm chart and helm-lints it (Helm ships on the runner); publish aca (task 070-007) does the
// same for the Azure Container Apps Bicep (no Azure credentials: publishing only writes files).
// publish aca-neon (task 317) does the same for Azure Container Apps with external Neon Postgres.
// Task 367: the workflow runs `dev publish all --no-build` — every publish target, then their
// safety suites in ONE aspire-tests run against the Release build that just ran, then helm lint.
// The test step runs `dev test --no-build` (task 365): the Release solution build just ran, so
// solution-listed test projects skip their own build/restore check.
// workflow.yml uploads every publish output (compose files, the Helm chart, aca Bicep, aca-neon
// Bicep; migration bundles stay out). No mode deploys anything (deploying is operator-run).
// Task 366: Pr/Merge splits into phases so workflow.yml runs them as parallel jobs, each on its
// own runner that builds what it needs (no shared build artifact: bin/obj is far larger than the
// org's 500 MB Actions storage cap). --phase test [--group <name>] is clean -> build -> test
// (one TestGroups group, or all); --phase publish is clean -> build -> publish all. No
// --phase runs everything in order, as before. Release mode accepts no --phase/--group: packing
// and pushing stay one job and one pass.
// The release path deliberately does not run tests — they already ran on the PR/merge that
// produced master. A release publishes as long as it builds. Publishing is gated only by an
// API key being supplied (--api-key, from OIDC Trusted Publishing); without one, pack-only
// and the post-publish template-publish-smoke gate is skipped (nothing was pushed). After a
// real push, template-publish-smoke waits for nuget.org flatcontainer, generates against the
// published template, asserts pins==version, restore+builds nuget.org-only — failure blocks
// the release (task 126-003). Handlers are invoked directly (no ./bin/dev dependency) so this
// runs in a clean CI checkout. PackableProjects includes source/libraries (TimeWarp.Modules,
// TimeWarp.Identity, TimeWarp.402), foundation, TimeWarp.Architecture.{Attributes,Analyzers,Generators}
// (task 092), and the template package; single Version from source/Directory.Build.props.
#endregion

namespace DevCli.Commands;

[NuruRoute("workflow", Description = "Execute full CI/CD pipeline (mode-aware)")]
internal sealed class WorkflowCommand : ICommand<Unit>
{
  [Option("mode", "m", Description = "CI mode: pr, merge, or release (auto-detected from GITHUB_EVENT_NAME)")]
  public string? Mode { get; set; }

  [Option("api-key", "k", Description = "NuGet API key for publishing (from OIDC Trusted Publishing)")]
  public string? ApiKey { get; set; }

  [Option("phase", "p", Description = "Pr/merge only: run one phase, test or publish (default: all)")]
  public string? Phase { get; set; }

  [Option("group", "g", Description = "With --phase test: run one test group, fast, playwright, or integration (default: all)")]
  public string? Group { get; set; }

  // The publishable set — single repo version (source/Directory.Build.props). libraries (modules,
  // identity, 402) ship as leaf product packages; foundation-application depends on modules; analyzers/
  // generators/attributes ship as platform compile-time packages (task 092); the template is the
  // dotnet-new package.
  internal static readonly string[] PackableProjects =
  [
    "source/libraries/timewarp-modules/timewarp-modules.csproj",
    "source/libraries/timewarp-identity/timewarp-identity.csproj",
    "source/libraries/timewarp-402/timewarp-402.csproj",
    "source/foundation/timewarp-contracts/timewarp-contracts.csproj",
    "source/foundation/foundation-domain/foundation-domain.csproj",
    "source/foundation/foundation-contracts/foundation-contracts.csproj",
    "source/foundation/foundation-application/foundation-application.csproj",
    "source/foundation/foundation-infrastructure/foundation-infrastructure.csproj",
    "source/foundation/foundation-server/foundation-server.csproj",
    "source/analyzers/timewarp-architecture-attributes/timewarp-architecture-attributes.csproj",
    "source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj",
    "source/analyzers/timewarp-architecture-analyzers/timewarp-architecture-analyzers.csproj",
    "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj",
  ];

  internal sealed class Handler : ICommandHandler<WorkflowCommand, Unit>
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

    public async Task<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Unit.Value;

      CiMode mode = DetermineMode(command.Mode);
      Terminal.WriteLine($"\nCI/CD Pipeline — Mode: {mode}\n".Cyan());

      if (!ValidatePhase(command, mode)) return Unit.Value;

      if (mode == CiMode.Release)
        await RunReleaseAsync(command.ApiKey);
      else
        await RunPrAsync(command.Phase ?? PhaseAll, command.Group);

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
    private const string PhaseTest = "test";
    private const string PhasePublish = "publish";

    private bool ValidatePhase(WorkflowCommand command, CiMode mode)
    {
      string? error = null;
      if (mode == CiMode.Release && (command.Phase is not null || command.Group is not null))
        error = "--phase and --group apply to pr/merge only; release runs as one job.";
      else if (command.Phase is not null && command.Phase is not (PhaseTest or PhasePublish))
        error = $"Unknown phase '{command.Phase}'. Phases: {PhaseTest}, {PhasePublish}.";
      else if (command.Group is not null && command.Phase != PhaseTest)
        error = "--group requires --phase test.";
      else if (command.Group is not null && !TestGroups.IsGroup(command.Group))
        error = $"Unknown test group '{command.Group}'. Groups: {string.Join(", ", TestGroups.Names)}.";

      if (error is null) return true;

      Terminal.WriteErrorLine($"Error: {error}".Red());
      Environment.ExitCode = 1;
      return false;
    }

    private CiMode DetermineMode(string? explicitMode)
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
      string steps = phase switch
      {
        PhaseTest => group is null ? "test" : $"test ({group})",
        PhasePublish => publishSteps,
        _ => $"test -> {publishSteps}",
      };
      Terminal.WriteLine($"Pipeline: clean -> build -> {steps}\n");
      Environment.ExitCode = 0;

      if (!await RunStepAsync("Clean", new CleanCommand.Handler(Terminal, RepoCleanService).Handle(new CleanCommand(), Ct))) return;
      if (!await RunStepAsync("Build", new BuildCommand.Handler(Terminal).Handle(new BuildCommand(), Ct))) return;
      if (test && !await RunStepAsync("Test", new TestCommand.Handler(Terminal).Handle(new TestCommand { NoBuild = true, Group = group }, Ct))) return;
      if (publish && !await RunStepAsync("Publish all", new PublishAllCommand.Handler(Terminal).Handle(new PublishAllCommand { NoBuild = true }, Ct))) return;

      Terminal.WriteLine("\nPipeline SUCCEEDED".Green());
    }

    // Release: NO test step — publish as long as it builds; post-push template-publish-smoke blocks on real publish.
    private async Task RunReleaseAsync(string? apiKey)
    {
      Terminal.WriteLine("Pipeline: clean -> build -> pack -> push -> template-publish-smoke\n");
      Environment.ExitCode = 0;

      if (!await RunStepAsync("Clean", new CleanCommand.Handler(Terminal, RepoCleanService).Handle(new CleanCommand(), Ct))) return;
      if (!await RunStepAsync("Build", new BuildCommand.Handler(Terminal).Handle(new BuildCommand(), Ct))) return;
      if (!await PackAsync()) return;

      string? resolvedKey = apiKey ?? Environment.GetEnvironmentVariable("NUGET_API_KEY");
      bool willPublish = !string.IsNullOrWhiteSpace(resolvedKey);
      if (!await PushAsync(apiKey)) return;

      if (willPublish)
      {
        if (!await RunStepAsync(
              "Template publish smoke",
              new TemplatePublishSmokeCommand.Handler(Terminal)
                .Handle(new TemplatePublishSmokeCommand(), Ct)))
        {
          return;
        }
      }

      Terminal.WriteLine("\nPipeline SUCCEEDED".Green());
    }

    private async Task<bool> PackAsync()
    {
      string outputDir = Path.Combine(RepoRoot, "artifacts", "packages");
      Directory.CreateDirectory(outputDir);

      foreach (string relativeProject in PackableProjects)
      {
        Terminal.WriteLine($"\nPacking {relativeProject}...");
        int exitCode = await DotNet.Pack(Path.Combine(RepoRoot, relativeProject))
          .WithConfiguration("Release")
          .WithOutput(outputDir)
          .WithNoValidation()
          .RunAsync(Ct);

        if (exitCode != 0)
        {
          Terminal.WriteErrorLine($"Pack failed for {relativeProject}!".Red());
          Environment.ExitCode = exitCode;
          return false;
        }
      }

      return true;
    }

    private async Task<bool> PushAsync(string? apiKey)
    {
      apiKey ??= Environment.GetEnvironmentVariable("NUGET_API_KEY");
      if (string.IsNullOrWhiteSpace(apiKey))
      {
        Terminal.WriteLine("\nNo API key supplied — pack-only (skipping push).".Yellow());
        return true;
      }

      string glob = Path.Combine(RepoRoot, "artifacts", "packages", "*.nupkg");
      Terminal.WriteLine($"\nPushing {glob} to nuget.org...");

      int exitCode = await DotNet.NuGet()
        .Push(glob)
        .WithApiKey(apiKey)
        .WithSource("https://api.nuget.org/v3/index.json")
        .WithSkipDuplicate()
        .RunAsync(Ct);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine("Push failed!".Red());
        Environment.ExitCode = exitCode;
        return false;
      }

      return true;
    }

    private async Task<bool> RunStepAsync(string stepName, Task<Unit> step)
    {
      await step;

      if (Environment.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"\nPipeline FAILED — {stepName} failed".Red());
        return false;
      }

      return true;
    }
  }
}

internal enum CiMode
{
  Pr,
  Merge,
  Release,
}
