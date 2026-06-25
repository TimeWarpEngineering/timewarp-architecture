#region Purpose
// Run the Aspire AppHost (replaces Run.ps1)
#endregion
#region Design
// Launches the AppHost via the first-party Aspire CLI (`aspire run`), the modern
// interactive-development entry point that handles build/restore, the dashboard, and
// graceful Ctrl+C shutdown. Uses full interactive passthrough and points at the AppHost
// explicitly with --apphost so discovery is deterministic. Sets
// ASPNETCORE_ENVIRONMENT=Development to match the legacy Run.ps1. Handler stores Ct and
// RepoRoot as fields so private methods are zero-parameter.
#endregion

namespace DevCli.Commands;

[NuruRoute("run", Description = "Run the Aspire AppHost (Development environment)")]
internal sealed class RunCommand : ICommand<Unit>
{
  private const string AppHostProject =
    "source/container-apps/aspire/aspire-app-host/aspire-app-host.csproj";

  internal sealed class Handler : ICommandHandler<RunCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(RunCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Value;
      await RunAsync();

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

    private async Task RunAsync()
    {
      string project = Path.Combine(RepoRoot, AppHostProject);

      Terminal.WriteLine($"Running Aspire AppHost from {project}...");
      ExecutionResult result = await Shell.Builder("aspire")
        .WithArguments("run", "--apphost", project)
        .WithWorkingDirectory(RepoRoot)
        .WithEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development")
        .WithNoValidation()
        .PassthroughAsync(Ct);

      if (!result.IsSuccess)
      {
        Terminal.WriteErrorLine($"AppHost exited with code {result.ExitCode}.".Red());
        Environment.ExitCode = result.ExitCode;
      }
    }
  }
}
