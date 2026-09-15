#region Purpose
// `dev entra disable`: set Authentication:Entra:Enabled=false in Web.Server user secrets.
#endregion

#region Design
// Local only — no Azure change. Idempotent. The app registration and other Entra secrets stay
// so `dev entra setup` without --new-secret can re-enable without minting a credential.
// Handler stores Command/Ct as fields so private methods are zero-parameter.
#endregion

namespace DevCli.Commands;

[NuruRoute("disable", Description = "Set Authentication:Entra:Enabled=false in Web.Server user secrets (no Azure change)")]
internal sealed class EntraDisableCommand : EntraGroup, ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<EntraDisableCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private EntraCli Cli = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(EntraDisableCommand command, CancellationToken ct)
    {
      if (!EntraCli.TryFindRepoRoot(Terminal, out string repoRoot))
      {
        Environment.ExitCode = 1;
        return Value;
      }

      Cli = new EntraCli(Terminal, repoRoot, dryRun: false, ct);
      CommandOutput output = await Cli
        .SetUserSecretAsync(EntraSetup.EnabledKey, "false", maskValue: false)
        .ConfigureAwait(false);
      if (!output.Success)
      {
        Cli.WriteFailure(output, "Failed to set Authentication:Entra:Enabled=false.");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine("Authentication:Entra:Enabled=false written to Web.Server user secrets.".Green());
      Terminal.WriteLine("Azure app registration was not changed. Re-enable with `dev entra setup`.");
      return Value;
    }
  }
}
