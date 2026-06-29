#region Purpose
// Pack and publish the repo's NuGet packages (foundation libraries + the dotnet-new template).
// This is the C# home for publish logic — the CI workflow is a thin trigger that just calls
//   dotnet run tools/dev-cli/dev.cs -- publish --target <foundation|template|all>
// passing a target based on which paths changed. All pack/push logic lives here, not in YAML.
#endregion
#region Design
// Version is the single repo-wide value from Directory.Version.props (no per-package versions).
// Packing honours that automatically; --skip-duplicate makes re-runs idempotent so publishing
// "all" on every push only pushes versions that don't already exist on the feed.
// API key resolution: --api-key flag wins, else the NUGET_API_KEY environment variable.
#endregion

namespace DevCli.Commands;

[NuruRoute("publish", Description = "Pack and publish NuGet packages (foundation, template, or all)")]
internal sealed class PublishCommand : ICommand<Unit>
{
  [Option("target", "t", Description = "What to publish: foundation | template | all (default: all)")]
  public string Target { get; set; } = "all";

  [Option("api-key", "k", Description = "NuGet API key (falls back to the NUGET_API_KEY env var)")]
  public string? ApiKey { get; set; }

  [Option("source", "s", Description = "NuGet push source")]
  public string Source { get; set; } = "https://api.nuget.org/v3/index.json";

  [Option("dry-run", "d", Description = "Pack only; do not push")]
  public bool DryRun { get; set; }

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  // Foundation packages (single repo-wide version). timewarp-modules ships too because
  // foundation-application depends on it.
  internal static readonly string[] FoundationProjects =
  [
    "source/libraries/timewarp-modules/timewarp-modules.csproj",
    "source/foundation/foundation-domain/foundation-domain.csproj",
    "source/foundation/foundation-contracts/foundation-contracts.csproj",
    "source/foundation/foundation-application/foundation-application.csproj",
    "source/foundation/foundation-infrastructure/foundation-infrastructure.csproj",
    "source/foundation/foundation-server/foundation-server.csproj",
  ];

  internal const string TemplateProject =
    "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj";

  internal sealed class Handler : ICommandHandler<PublishCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private PublishCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string OutputDir = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(PublishCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;

      if (!FindRepoRoot()) return Value;

      string[] projects = ResolveProjects();
      if (projects.Length == 0) return Value;

      OutputDir = Path.Combine(RepoRoot, "artifacts", "packages");
      Directory.CreateDirectory(OutputDir);

      foreach (string project in projects)
        if (!await PackAsync(project)) return Value;

      if (Command.DryRun)
      {
        Terminal.WriteLine($"\nDry run: packed {projects.Length} project(s) to {OutputDir} (no push).".Green());
        return Value;
      }

      if (!await PushAsync()) return Value;

      Terminal.WriteLine("\nPublish completed successfully!".Green());
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

    private string[] ResolveProjects()
    {
      string target = Command.Target.Trim().ToLowerInvariant();
      string[] projects = target switch
      {
        "foundation" => FoundationProjects,
        "template" => [TemplateProject],
        "all" => [.. FoundationProjects, TemplateProject],
        _ => [],
      };

      if (projects.Length == 0)
      {
        Terminal.WriteErrorLine($"Error: unknown --target '{Command.Target}'. Use foundation, template, or all.");
        Environment.ExitCode = 1;
      }
      else
      {
        Terminal.WriteLine($"Publishing target '{target}' ({projects.Length} project(s)) from {RepoRoot}...");
      }

      return projects;
    }

    private async Task<bool> PackAsync(string relativeProject)
    {
      string project = Path.Combine(RepoRoot, relativeProject);

      Terminal.WriteLine($"\nPacking {relativeProject}...");
      CommandOutput result = await DotNet.Pack(project)
        .WithConfiguration("Release")
        .WithOutput(OutputDir)
        .WithNoValidation()
        .CaptureAsync(Ct);

      return ReportResult(result, $"Pack failed for {relativeProject}!");
    }

    private async Task<bool> PushAsync()
    {
      string? apiKey = Command.ApiKey ?? Environment.GetEnvironmentVariable("NUGET_API_KEY");
      if (string.IsNullOrWhiteSpace(apiKey))
      {
        Terminal.WriteErrorLine("Error: no API key. Pass --api-key or set NUGET_API_KEY.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      string glob = Path.Combine(OutputDir, "*.nupkg");
      Terminal.WriteLine($"\nPushing {glob} to {Command.Source}...");

      CommandOutput result = await DotNet.NuGet()
        .Push(glob)
        .WithApiKey(apiKey)
        .WithSource(Command.Source)
        .WithSkipDuplicate()
        .CaptureAsync(Ct);

      return ReportResult(result, "Push failed!");
    }

    private bool ReportResult(CommandOutput result, string failureMessage)
    {
      if (Command.Verbose)
        Terminal.WriteLine(result.Combined);

      if (!result.Success)
      {
        if (!Command.Verbose)
          Terminal.WriteErrorLine(result.Combined);
        Terminal.WriteErrorLine(failureMessage.Red());
        Environment.ExitCode = 1;
        return false;
      }

      return true;
    }
  }
}
