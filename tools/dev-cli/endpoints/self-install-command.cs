#region Purpose
// Installs the dev CLI executable locally
#endregion
#region Design
// Uses dotnet publish to create the binary in ./bin
// Handler stores Ct and RepoRoot as fields so private methods are zero-parameter
#endregion

namespace DevCli.Commands;

[NuruRoute("self-install", Description = "AOT compile dev CLI to ./bin")]
internal sealed class SelfInstallCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<SelfInstallCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(SelfInstallCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Value;
      if (!await PublishAsync()) return Value;

      Terminal.WriteLine($"Successfully installed dev CLI to {Path.Combine(RepoRoot, "bin")}".Green());
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

    private async Task<bool> PublishAsync()
    {
      string devCliPath = Path.Combine(RepoRoot, "tools", "dev-cli", "dev.cs");
      string outputDir = Path.Combine(RepoRoot, "bin");

      Terminal.WriteLine("Publishing dev CLI...");
      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("publish", devCliPath, "-o", outputDir)
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (!result.Success)
      {
        Terminal.WriteErrorLine(result.Combined);
        Terminal.WriteErrorLine("Self-install failed!".Red());
        Environment.ExitCode = 1;
        return false;
      }

      return true;
    }
  }
}
