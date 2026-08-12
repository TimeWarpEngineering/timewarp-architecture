#region Purpose
// Show EF migration status for the app database via the running AppHost.
#endregion

#region Design
// Aspire web-migrations ef-database-status. Non-destructive.
#endregion

namespace DevCli.Commands;

[NuruRoute("status", Description = "Show current EF migration status via the running AppHost")]
internal sealed class DbStatusCommand : DbGroup, ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<DbStatusCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(DbStatusCommand command, CancellationToken ct)
    {
      int exit = await DbAppHost.RunMigrationsCommandAsync(
        Terminal,
        DbAppHost.CommandStatus,
        $"Database status via '{DbAppHost.MigrationsResourceName}'...",
        ct).ConfigureAwait(false);

      if (exit != 0)
      {
        Environment.ExitCode = exit;
      }

      return Value;
    }
  }
}
