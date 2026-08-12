#region Purpose
// Apply pending EF migrations on demand via the running AppHost (task 159)
#endregion
#region Design
// Migrations are explicit/on-demand in every environment (task 155 hybrid design): production
// applies published script/bundle artifacts; local dev gets RunDatabaseUpdateOnStart on AppHost
// start plus this command for an explicit re-run without touching the dashboard. Implemented as
// `aspire resource web-migrations ef-database-update` against the RUNNING AppHost rather than a
// cold `dotnet ef` invocation because the Aspire postgres container has a dynamic port — only
// the AppHost knows the live connection string. --apphost pins discovery to this repo's AppHost
// so a second AppHost running on the machine can never be targeted by accident.
// The resource name string must equal WebMigrationsResourceName in the AppHost's constants.cs
// (agreement-by-memory across projects; the CLI cannot reference the AppHost assembly).
// The web-migrations resource only exists when the postgres template flag is on; without it the
// aspire CLI reports the resource as unknown, which is the honest error already.
#endregion

namespace DevCli.Commands;

[NuruRoute("db-update", Description = "Apply pending EF migrations via the running AppHost (web-migrations ef-database-update)")]
internal sealed class DbUpdateCommand : ICommand<Unit>
{
  private const string AppHostProject =
    "source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj";

  private const string MigrationsResourceName = "web-migrations"; // = constants.cs WebMigrationsResourceName

  internal sealed class Handler : ICommandHandler<DbUpdateCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(DbUpdateCommand command, CancellationToken ct)
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

      Terminal.WriteLine($"Applying pending EF migrations via '{MigrationsResourceName}' on the running AppHost...");
      CommandOutput result = await Shell.Builder("aspire")
        .WithArguments("resource", MigrationsResourceName, "ef-database-update", "--apphost", project)
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .PassthroughAsync(Ct);

      if (!result.Success)
      {
        Terminal.WriteErrorLine($"db-update failed with code {result.ExitCode}.".Red());
        Terminal.WriteErrorLine("Is the AppHost running? Start it with `dev run` first — this command executes against the live resource graph.");
        Environment.ExitCode = result.ExitCode;
      }
    }
  }
}
