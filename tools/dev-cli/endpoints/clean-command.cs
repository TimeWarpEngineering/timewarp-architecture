#region Purpose
// Clean the solution and all build artifacts
#endregion
#region Design
// Executes dotnet restore, dotnet clean, then removes bin/obj folders via Directory.Delete
// Handler stores Ct and RepoRoot as fields so private methods are zero-parameter
// Streams dotnet output by default; --quiet captures and hides success output
#endregion

namespace DevCli.Commands;

[NuruRoute("clean", Description = "Clean solution and build artifacts")]
internal sealed class CleanCommand : ICommand<Unit>
{
  [Option("quiet", "q", Description = "Hide clean output unless the command fails")]
  public bool Quiet { get; set; }

  internal sealed class Handler : ICommandHandler<CleanCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CleanCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(CleanCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;

      if (!FindRepoRoot()) return Value;
      if (!await RestoreAsync()) return Value;
      if (!await CleanAsync()) return Value;
      DeleteBinObjDirectories();

      Terminal.WriteLine("\nClean completed successfully!".Green());
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
      Terminal.WriteLine($"Cleaning repository at {RepoRoot}...");
      return true;
    }

    private async Task<bool> RestoreAsync()
    {
      Terminal.WriteLine("\nRestoring packages...");
      DotNetRestoreBuilder builder = DotNet.Restore()
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation();

      if (Command.Quiet)
      {
        CommandOutput capture = await builder.CaptureAsync(Ct);
        return CommandExecution.ReportCapture(Terminal, capture, "dotnet restore failed!");
      }

      ExecutionResult execution = await builder.PassthroughAsync(Ct);
      return CommandExecution.ReportPassthrough(Terminal, execution, "dotnet restore failed!");
    }

    private async Task<bool> CleanAsync()
    {
      Terminal.WriteLine("\nCleaning solution...");
      DotNetCleanBuilder builder = DotNet.Clean()
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation();

      if (Command.Quiet)
      {
        CommandOutput capture = await builder.CaptureAsync(Ct);
        return CommandExecution.ReportCapture(Terminal, capture, "dotnet clean failed!");
      }

      ExecutionResult execution = await builder.PassthroughAsync(Ct);
      return CommandExecution.ReportPassthrough(Terminal, execution, "dotnet clean failed!");
    }

    private void DeleteBinObjDirectories()
    {
      Terminal.WriteLine("\nDeleting obj and bin directories...");
      string[] dirs = Directory.GetDirectories(RepoRoot, "*", SearchOption.AllDirectories)
        .Where
        (
          d =>
          {
            string name = Path.GetFileName(d);
            return StringComparer.OrdinalIgnoreCase.Equals(name, "bin")
              || StringComparer.OrdinalIgnoreCase.Equals(name, "obj");
          }
        )
        .ToArray();

      foreach (string dir in dirs)
      {
        try
        {
          Directory.Delete(dir, recursive: true);
        }
        catch (Exception ex)
        {
          Terminal.WriteLine($"Warning: could not delete {dir}: {ex.Message}".Yellow());
        }
      }

      Terminal.WriteLine("Deleted all obj and bin directories.");
    }
  }
}