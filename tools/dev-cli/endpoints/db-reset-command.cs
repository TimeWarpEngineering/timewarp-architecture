#region Purpose
// Greenfield wipe: drop and recreate the app database with all EF migrations applied.
#endregion

#region Design
// Maps to Aspire web-migrations ef-database-reset. Requires --yes. Empty store after reset —
// next Create account claims Administrator. Not a product migration path for old principals.
#endregion

namespace DevCli.Commands;

[NuruRoute("reset", Description = "Drop and recreate the database with all migrations (requires --yes)")]
internal sealed class DbResetCommand : DbGroup, ICommand<Unit>
{
  [Option("yes", "y", Description = "Confirm destructive reset (required)")]
  public bool Yes { get; set; }

  internal sealed class Handler : ICommandHandler<DbResetCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(DbResetCommand command, CancellationToken ct)
    {
      if (!command.Yes)
      {
        Terminal.WriteErrorLine("Refusing to reset without --yes (drops all data, re-applies migrations).".Red());
        Terminal.WriteLine("Usage: dev db reset --yes");
        Terminal.WriteLine("Requires a running AppHost (`dev run`). After reset, Create account claims Administrator.");
        Environment.ExitCode = 1;
        return Value;
      }

      int exit = await DbAppHost.RunMigrationsCommandAsync(
        Terminal,
        DbAppHost.CommandReset,
        $"Resetting database via '{DbAppHost.MigrationsResourceName}' (drop + migrate)...",
        ct).ConfigureAwait(false);

      if (exit != 0)
      {
        Environment.ExitCode = exit;
        return Value;
      }

      Terminal.WriteLine("Database reset complete.".Green());
      Terminal.WriteLine("Empty store — next Create account claims Administrator.");
      return Value;
    }
  }
}
