#region Purpose
// Build all projects in the repository
#endregion
#region Design
// Discovers the repository root dynamically using Git.FindRoot()
// Handler stores Command and Ct as fields so private methods are zero-parameter
// Captures MSBuild output via Amuru CaptureAsync by default: a green build prints one summary line,
// a failed step prints the whole captured output to stderr. --verbose streams via RunAsync.
// --quiet is the default and stays accepted. Mode rules and the summary: services/build-output.cs.
// dev workflow (CI) builds with the defaults, so a red CI build still shows every diagnostic.
// Before MSBuild, scan tracked files for client names (the same scan as dev check-client-names).
// CI's fast job runs this build. A skills-only PR does not, so dev lint-skills runs the scan too.
// ganda repo audit has no word-list check.
#endregion

namespace DevCli.Commands;

using System.Diagnostics;

[NuruRoute("build", Description = "Build all projects in the repository")]
internal sealed class BuildCommand : ICommand<Unit>
{
  [Option("clean", "c", Description = "Clean before building")]
  public bool Clean { get; set; }

  [Option("verbose", "v", Description = "Stream full MSBuild output while building")]
  public bool Verbose { get; set; }

  [Option("quiet", "q", Description = "Hide MSBuild output unless the build fails (the default)")]
  public bool Quiet { get; set; }

  internal sealed class Handler : IRequestHandler<BuildCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private BuildCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private BuildOutputMode Mode;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;
      Mode = BuildOutput.Select(command.Verbose, command.Quiet);
      var stopwatch = Stopwatch.StartNew();

      if (!FindRepoRoot()) return Unit.Value;
      if (!await ClientNameScan.ReportAsync(
            RepoRoot,
            message => Terminal.WriteLine(message),
            message => Terminal.WriteErrorLine(message.Red()),
            Ct))
      {
        return Unit.Value;
      }

      if (!await CleanAsync()) return Unit.Value;
      if (!await BuildAsync()) return Unit.Value;

      Terminal.WriteLine(Mode == BuildOutputMode.Stream
        ? $"\n{BuildOutput.SuccessText}".Green()
        : BuildOutput.SuccessSummary(RepoRoot, stopwatch.Elapsed).Green());
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
      Terminal.WriteLine($"Building repository at {RepoRoot}...");
      return true;
    }

    private async Task<bool> CleanAsync()
    {
      if (!Command.Clean) return true;

      string solutionFile = Path.Combine(RepoRoot, "timewarp-architecture.slnx");

      WriteStepLine($"Cleaning {solutionFile}...");
      CommandResult command = DotNet.Clean()
        .WithProject(solutionFile)
        .WithNoValidation()
        .Build();

      return await ExecuteAsync(command, "Clean failed!");
    }

    private async Task<bool> BuildAsync()
    {
      string solutionFile = Path.Combine(RepoRoot, "timewarp-architecture.slnx");

      WriteStepLine($"Building {solutionFile}...");
      CommandResult command = DotNet.Build()
        .WithProject(solutionFile)
        .WithConfiguration("Release")
        .WithNoValidation()
        .Build();

      return await ExecuteAsync(command, "Build failed!");
    }

    // Streamed output needs a blank line between steps; captured output prints the step line alone.
    private void WriteStepLine(string message) =>
      Terminal.WriteLine(Mode == BuildOutputMode.Stream ? "\n" + message : message);

    private async Task<bool> ExecuteAsync(CommandResult command, string failureMessage)
    {
      if (Mode == BuildOutputMode.Capture)
      {
        CommandOutput result = await command.CaptureAsync(Ct);
        if (!result.Success)
        {
          Terminal.WriteErrorLine(result.Combined);
          Terminal.WriteErrorLine(failureMessage.Red());
          Environment.ExitCode = 1;
          return false;
        }

        return true;
      }

      int exitCode = await command.RunAsync(Ct);
      if (exitCode != 0)
      {
        Terminal.WriteErrorLine(failureMessage.Red());
        Environment.ExitCode = exitCode;
        return false;
      }

      return true;
    }
  }
}