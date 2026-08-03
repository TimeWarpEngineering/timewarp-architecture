#region Purpose
// Full CI/CD pipeline, mode-aware (mirrors the TimeWarp.Nuru / TimeWarp.Amuru pattern).
#endregion
#region Design
// Mode is auto-detected from GITHUB_EVENT_NAME (or forced with --mode):
//   pull_request / push  -> Pr/Merge:  clean -> build -> test        (tests are the gate)
//   release / dispatch   -> Release:   clean -> build -> pack -> push -> template-publish-smoke
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

    public async ValueTask<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Value;

      CiMode mode = DetermineMode(command.Mode);
      Terminal.WriteLine($"\nCI/CD Pipeline — Mode: {mode}\n".Cyan());

      if (mode == CiMode.Release)
        await RunReleaseAsync(command.ApiKey);
      else
        await RunPrAsync();

      return Value;
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
    private async Task RunPrAsync()
    {
      Terminal.WriteLine("Pipeline: clean -> build -> test\n");
      Environment.ExitCode = 0;

      if (!await RunStepAsync("Clean", new CleanCommand.Handler(Terminal, RepoCleanService).Handle(new CleanCommand(), Ct))) return;
      if (!await RunStepAsync("Build", new BuildCommand.Handler(Terminal).Handle(new BuildCommand(), Ct))) return;
      if (!await RunStepAsync("Test", new TestCommand.Handler(Terminal).Handle(new TestCommand(), Ct))) return;

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
