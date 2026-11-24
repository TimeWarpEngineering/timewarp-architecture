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
//   release / dispatch   -> Release:   clean -> build -> pack -> push -> template-publish-smoke
// Pr/Merge splits into phases so workflow.yml runs them as parallel jobs (ci (fast), ci
// (integration), ci (publish)), each on its own runner that builds what it needs: a shared build
// artifact would be bin/obj for the whole solution, far over the org's 500 MB Actions storage cap.
// --phase test [--group <name>] is clean -> build -> test (one TestGroups group, or all); --phase
// publish is clean -> build -> publish all. No --phase runs everything in order. Release mode
// accepts no --phase/--group: packing and pushing stay one job and one pass. The playwright group
// is not in the CI matrix; `dev test --group playwright` still runs it.
// The test step runs `dev test --no-build`, and publish runs `dev publish all --no-build` (every
// target, their safety suites in one aspire-tests run, then helm lint), against the Release build
// that just ran. Publish outputs are files on the runner; CI uploads none of them and deploys
// nothing (deploying is operator-run).
//
// Whenever the test step includes the fast group (the fast job, or a full local run), the ingress
// prefix golden is regenerated from the two host builds and goldens/ingress must show no diff and
// no untracked file. A hand-edited golden, or a contract change that skipped regenerate, fails here.
//
// --phase classify is the whole of workflow.yml's classify job. It writes three outputs:
//   ci        PullRequestBuildClassifier.RequiresBuild (TimeWarp.Nuru.DevCli) with this repo's
//             additions in .timewarp/dev.jsonc pullRequestBuild, over the same changed paths. A
//             push applies the pull request rule to its own diff (DecideAsync builds for every
//             event but pull_request), so a skills-only push skips ci-group as a skills-only pull
//             request does. An unreadable dev.jsonc on a push builds.
//   skill     WorkflowPathClasses.IsSkillPath over the changed paths (gates skill-lint).
//   template  WorkflowPathClasses.IsTemplatePath over the changed paths (gates template-smoke).
// A pull request takes the classifier's base...head diff (the job checks out full history). A push
// fetches its before and after commits when missing (--depth=1 only on a shallow checkout, so a
// local full clone never turns shallow) and diffs their trees. Any diff that cannot be read
// (new branch, force push, failed fetch, local run) turns every job on. The test and publish phases
// do not classify: their shallow checkout cannot diff, and the classify job already gated them.
//
// Checkouts are shallow everywhere but classify on a pull request: SourceLink and
// TimeWarp.Build.Tasks read only HEAD, `dev check-version` compares against nuget.org, and
// `dev release` (the only tag writer) runs locally.
//
// The release path deliberately does not run tests — they already ran on the PR/merge that
// produced master. A release publishes as long as it builds. Publishing is gated only by an
// API key being supplied (--api-key, from OIDC Trusted Publishing); without one, pack-only
// and the post-publish template-publish-smoke gate is skipped (nothing was pushed). A dispatch
// therefore never publishes, and needs no break-glass confirm input. After a real push,
// template-publish-smoke waits for nuget.org flatcontainer, generates against the published
// template, asserts pins==version, restore+builds nuget.org-only — failure blocks the release.
// Handlers are invoked directly (no ./bin/dev dependency) so this runs in a clean CI checkout.
// PackableProjects includes source/libraries (TimeWarp.Modules, TimeWarp.Identity, TimeWarp.402),
// foundation, TimeWarp.Architecture.{Attributes,Analyzers,Generators}, and the template package;
// single Version from source/Directory.Build.props. TimeWarp.Contracts is a PackageReference at
// the Directory.Packages.props pin. This list does not pack it.
#endregion

namespace DevCli.Commands;

using System.Text.Json;

[NuruRoute("workflow", Description = "Execute full CI/CD pipeline (mode-aware)")]
internal sealed class WorkflowCommand : ICommand<Unit>
{
  [Option("mode", "m", Description = "CI mode: pr, merge, or release (auto-detected from GITHUB_EVENT_NAME)")]
  public string? Mode { get; set; }

  [Option("api-key", "k", Description = "NuGet API key for publishing (from OIDC Trusted Publishing)")]
  public string? ApiKey { get; set; }

  [Option("phase", "p", Description = "Pr/merge only: run one phase, classify, test, or publish (default: all)")]
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

      CiMode mode = DetermineMode(command.Mode);
      Terminal.WriteLine($"\nCI/CD Pipeline — Mode: {mode}\n".Cyan());

      if (!ValidatePhase(command, mode)) return Unit.Value;

      if (mode == CiMode.Release)
      {
        await RunReleaseAsync(command.ApiKey);
        return Unit.Value;
      }

      string phase = command.Phase ?? PhaseAll;
      if (phase is PhaseAll or PhaseClassify)
      {
        PullRequestBuildDecision decision = await PullRequestBuildClassifier.DecideAsync(RepoRoot, ct);
        Terminal.WriteLine(decision.Message);
        if (phase == PhaseClassify)
        {
          await ClassifyAsync(decision);
          return Unit.Value;
        }

        if (!decision.RequiresBuild) return Unit.Value;
      }

      await RunPrAsync(phase, command.Group);
      return Unit.Value;
    }

    // Writes ci, skill, and template to GITHUB_OUTPUT. A diff that cannot be read turns every job on.
    private async Task ClassifyAsync(PullRequestBuildDecision decision)
    {
      IReadOnlyList<string>? changed = await ReadChangedPathsAsync(decision);
      if (changed is null)
      {
        Terminal.WriteLine("Changed paths unknown. Every path-gated job runs.");
      }
      else
      {
        Terminal.WriteLine($"Changed paths ({changed.Count}):");
        foreach (string path in changed) Terminal.WriteLine($"  {path}");
      }

      (bool configRead, PullRequestBuildConfig? config) = changed is null ? (true, null) : await ReadBuildConfigAsync();
      WorkflowJobs jobs = WorkflowPathClasses.Classify(
        changed,
        paths => !configRead || PullRequestBuildClassifier.RequiresBuild(paths, config));
      (string Name, bool Value)[] outputs = [("ci", jobs.Ci), ("skill", jobs.Skill), ("template", jobs.Template)];

      string? outputPath = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
      foreach ((string name, bool value) in outputs)
      {
        string line = $"{name}={(value ? "true" : "false")}";
        Terminal.WriteLine(line);
        if (!string.IsNullOrEmpty(outputPath)) await File.AppendAllTextAsync(outputPath, line + "\n", Ct);
      }
    }

    // pullRequestBuild from .timewarp/dev.jsonc, the same additions DecideAsync reads. False when
    // the file exists but cannot be read: the caller then builds.
    private async Task<(bool Read, PullRequestBuildConfig? Config)> ReadBuildConfigAsync()
    {
      string path = Path.Combine(RepoRoot, BuildConfigFile);
      if (!File.Exists(path)) return (true, null);

      try
      {
        return (true, RepoConfigService.Parse(await File.ReadAllTextAsync(path, Ct)).PullRequestBuild);
      }
      catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
      {
        Terminal.WriteErrorLine($"{BuildConfigFile} could not be read ({exception.Message}). ci runs.");
        return (false, null);
      }
    }

    private const string BuildConfigFile = ".timewarp/dev.jsonc";

    // Pull request: the classifier's diff (empty with a build decision means it was not read).
    // Push: fetch before and after (the checkout is shallow) and diff their trees. Else unknown.
    private async Task<IReadOnlyList<string>?> ReadChangedPathsAsync(PullRequestBuildDecision decision)
    {
      string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
      if (eventName == PullRequestBuildClassifier.PullRequestEventName)
      {
        return !decision.RequiresBuild || decision.ChangedPaths.Count > 0 ? decision.ChangedPaths : null;
      }

      if (eventName != "push") return null;

      string? eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
      string? json = File.Exists(eventPath) ? await File.ReadAllTextAsync(eventPath, Ct) : null;
      if (!WorkflowPathClasses.TryReadPushShas(json, out string before, out string after)) return null;

      if (!await HasCommitsAsync(before, after))
      {
        // --depth only on a shallow checkout (CI); on a full clone it would make the clone shallow.
        CommandOutput shallow = await GitCommand("rev-parse", "--is-shallow-repository").CaptureAsync(Ct);
        string[] fetch = shallow.Stdout.Trim() == "true"
          ? ["fetch", "--no-tags", "--depth=1", "origin", before, after]
          : ["fetch", "--no-tags", "origin", before, after];
        if (!(await GitCommand(fetch).CaptureAsync(Ct)).Success) return null;
      }

      CommandOutput diff = await GitCommand("diff", "--name-only", before, after).CaptureAsync(Ct);
      if (!diff.Success) return null;

      return diff.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private async Task<bool> HasCommitsAsync(params string[] shas)
    {
      foreach (string sha in shas)
      {
        if (!(await GitCommand("cat-file", "-e", sha + "^{commit}").CaptureAsync(Ct)).Success) return false;
      }

      return true;
    }

    private ShellBuilder GitCommand(params string[] arguments) =>
      Shell.Builder("git").WithArguments(arguments).WithWorkingDirectory(RepoRoot).WithNoValidation();

    // Regenerates the ingress golden, then fails on any tracked diff or untracked file under it.
    private async Task<bool> VerifyIngressGoldenAsync()
    {
      if (!await RunStepAsync("Regenerate ingress golden", new RegenerateGeneratedCommand.Handler(Terminal).Handle(new RegenerateGeneratedCommand(), Ct))) return false;

      CommandOutput diff = await GitCommand("diff", "--exit-code", "--", IngressGoldenDirectory).PassthroughAsync(Ct);
      CommandOutput status = await GitCommand("status", "--porcelain", "--", IngressGoldenDirectory).CaptureAsync(Ct);
      if (diff.Success && status.Success && string.IsNullOrWhiteSpace(status.Stdout)) return true;

      Terminal.WriteErrorLine($"{IngressGoldenDirectory} changed after regenerate-generated. Run `dev regenerate-generated` and commit the golden.".Red());
      if (!string.IsNullOrWhiteSpace(status.Stdout)) Terminal.WriteErrorLine(status.Stdout);
      Terminal.WriteErrorLine("\nPipeline FAILED — Ingress golden check failed".Red());
      Environment.ExitCode = 1;
      return false;
    }

    private const string IngressGoldenDirectory = "goldens/ingress";

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
      bool golden = test && (group is null or TestGroups.Fast);
      const string publishSteps = "publish all";
      string testSteps = (group is null ? "test" : $"test ({group})") + (golden ? " -> ingress golden" : "");
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
      if (golden && !await VerifyIngressGoldenAsync()) return;
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

    private async Task<bool> RunStepAsync(string stepName, ValueTask<Unit> step)
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
