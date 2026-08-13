#region Purpose
// Drop the app database via the running AppHost (no recreate unless you then db update).
#endregion

#region Design
// Aspire web-migrations ef-database-drop. Requires --yes. Prefer `db reset` for drop+migrate.
#endregion

namespace DevCli.Commands;

[NuruRoute("drop", Description = "Delete the database (requires --yes; prefer 'db reset' for drop+migrate)")]
internal sealed class DbDropCommand : DbGroup, ICommand<Unit>
{
  [Option("yes", "y", Description = "Confirm destructive drop (required)")]
  public bool Yes { get; set; }

  internal sealed class Handler : ICommandHandler<DbDropCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(DbDropCommand command, CancellationToken ct)
    {
      if (!command.Yes)
      {
        Terminal.WriteErrorLine("Refusing to drop without --yes.".Red());
        Terminal.WriteLine("Usage: dev db drop --yes");
        Terminal.WriteLine("Prefer `dev db reset --yes` to drop and re-apply migrations in one step.");
        Environment.ExitCode = 1;
        return Value;
      }

      int exit = await DbAppHost.RunMigrationsCommandAsync(
        Terminal,
        DbAppHost.CommandDrop,
        $"Dropping database via '{DbAppHost.MigrationsResourceName}'...",
        ct).ConfigureAwait(false);

      if (exit != 0)
      {
        Environment.ExitCode = exit;
        return Value;
      }

      Terminal.WriteLine("Database dropped.".Green());
      Terminal.WriteLine("Run `dev db update` (or restart AppHost) to recreate schema.");
      return Value;
    }
  }
}
