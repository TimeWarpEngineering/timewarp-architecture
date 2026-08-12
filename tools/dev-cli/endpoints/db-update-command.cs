#region Purpose
// Apply pending EF migrations via the running AppHost (`dev db update` + legacy `db-update` alias).
#endregion

#region Design
// Task 159 original flat command, folded under `db` group. Alias class keeps `dev db-update`
// muscle memory. Implementation is aspire resource web-migrations ef-database-update.
#endregion

namespace DevCli.Commands;

[NuruRoute("update", Description = "Apply pending EF migrations via the running AppHost")]
internal sealed class DbUpdateCommand : DbGroup, ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<DbUpdateCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(DbUpdateCommand command, CancellationToken ct)
    {
      Environment.ExitCode = await DbAppHost.RunUpdateAsync(Terminal, ct).ConfigureAwait(false);
      return Value;
    }
  }
}

/// <summary>Flat alias so <c>dev db-update</c> still works.</summary>
[NuruRoute("db-update", Description = "Alias for 'db update'")]
internal sealed class DbUpdateAliasCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<DbUpdateAliasCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(DbUpdateAliasCommand command, CancellationToken ct)
    {
      Environment.ExitCode = await DbAppHost.RunUpdateAsync(Terminal, ct).ConfigureAwait(false);
      return Value;
    }
  }
}
