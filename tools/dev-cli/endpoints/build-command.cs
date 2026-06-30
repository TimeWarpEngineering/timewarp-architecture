#region Purpose
// Build all projects in the repository
#endregion
#region Design
// Discovers the repository root dynamically using Git.FindRoot()
// Handler stores Command and Ct as fields so private methods are zero-parameter
// Streams MSBuild output by default (PassthroughAsync); --quiet captures and hides success output
#endregion

namespace DevCli.Commands;

[NuruRoute("build", Description = "Build all projects in the repository")]
internal sealed class BuildCommand : ICommand<Unit>
{
  [Option("clean", "c", Description = "Clean before building")]
  public bool Clean { get; set; }

  [Option("quiet", "q", Description = "Hide build output unless the command fails")]
  public bool Quiet { get; set; }

  internal sealed class Handler : ICommandHandler<BuildCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private BuildCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;

      if (!FindRepoRoot()) return Value;
      if (!await CleanAsync()) return Value;
      if (!await BuildAsync()) return Value;

      Terminal.WriteLine("\nBuild completed successfully!".Green());
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
      Terminal.WriteLine($"Building repository at {RepoRoot}...");
      return true;
    }

    private async Task<bool> CleanAsync()
    {
      if (!Command.Clean) return true;

      string solutionFile = Path.Combine(RepoRoot, "timewarp-architecture.slnx");

      Terminal.WriteLine($"\nCleaning {solutionFile}...");
      DotNetCleanBuilder builder = DotNet.Clean()
        .WithProject(solutionFile)
        .WithNoValidation();

      if (Command.Quiet)
      {
        CommandOutput capture = await builder.CaptureAsync(Ct);
        return CommandExecution.ReportCapture(Terminal, capture, "Clean failed!");
      }

      ExecutionResult execution = await builder.PassthroughAsync(Ct);
      return CommandExecution.ReportPassthrough(Terminal, execution, "Clean failed!");
    }

    private async Task<bool> BuildAsync()
    {
      string solutionFile = Path.Combine(RepoRoot, "timewarp-architecture.slnx");

      Terminal.WriteLine($"\nBuilding {solutionFile}...");
      DotNetBuildBuilder builder = DotNet.Build()
        .WithProject(solutionFile)
        .WithConfiguration("Release")
        .WithNoValidation();

      if (Command.Quiet)
      {
        CommandOutput capture = await builder.CaptureAsync(Ct);
        return CommandExecution.ReportCapture(Terminal, capture, "Build failed!");
      }

      ExecutionResult execution = await builder.PassthroughAsync(Ct);
      return CommandExecution.ReportPassthrough(Terminal, execution, "Build failed!");
    }
  }
}