#region Purpose
// Build all projects in the repository
#endregion
#region Design
// Discovers the repository root dynamically using Git.FindRoot()
// Handler stores Command and Ct as fields so private methods are zero-parameter
// ReportResult handles verbose/error output consistently across steps
#endregion

namespace DevCli.Commands;

[NuruRoute("build", Description = "Build all projects in the repository")]
internal sealed class BuildCommand : ICommand<Unit>
{
  [Option("clean", "c", Description = "Clean before building")]
  public bool Clean { get; set; }

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

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

      Terminal.WriteLine("\nCleaning before build...");
      CommandOutput result = await DotNet.Clean()
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);

      return ReportResult(result, "Clean failed!");
    }

    private async Task<bool> BuildAsync()
    {
      Terminal.WriteLine("\nBuilding...");
      CommandOutput result = await DotNet.Build()
        .WithConfiguration("Release")
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);

      return ReportResult(result, "Build failed!");
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
