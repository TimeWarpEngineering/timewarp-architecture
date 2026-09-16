#region Purpose
// `dev entra reseed`: toggle Authentication:Entra:ReseedSiteSettings in Web.Server user secrets.
#endregion

#region Design
// Does not mutate Azure or drop the database. Sets ReseedSiteSettings=true so the next
// Development boot of Web.Server overwrites EntraSignInEnabled and EntraAllowBootstrap
// from configuration (PasskeyPromptMode is kept). The seeder ignores the
// flag outside Development. --clear writes false after a successful reseed so later boots keep
// admin edits. Prints `dev db reset --yes` as the blunt fallback that drops principals and
// passkeys. Handler stores Command/Ct as fields so private methods are zero-parameter.
#endregion

namespace DevCli.Commands;

[NuruRoute("reseed", Description = "Set Authentication:Entra:ReseedSiteSettings for the next Development Web.Server boot")]
[NuruRouteExample("entra reseed", Description = "Write ReseedSiteSettings=true so the next Development boot overwrites Entra policy from configuration")]
[NuruRouteExample("entra reseed --clear", Description = "Write ReseedSiteSettings=false after a successful reseed")]
internal sealed class EntraReseedCommand : EntraGroup, ICommand<Unit>
{
  [Option("clear", Description = "Set ReseedSiteSettings=false so later boots keep admin edits")]
  public bool Clear { get; set; }

  internal sealed class Handler : ICommandHandler<EntraReseedCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private EntraReseedCommand Command = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(EntraReseedCommand command, CancellationToken ct)
    {
      Command = command;

      if (!EntraCli.TryFindRepoRoot(Terminal, out string repoRoot))
      {
        Environment.ExitCode = 1;
        return Value;
      }

      EntraCli cli = new(Terminal, repoRoot, dryRun: false, ct);
      string value = Command.Clear ? "false" : "true";
      CommandOutput output = await cli
        .SetUserSecretAsync(EntraSetup.ReseedSiteSettingsKey, value, maskValue: false)
        .ConfigureAwait(false);
      if (!output.Success)
      {
        cli.WriteFailure(output, $"Failed to set {EntraSetup.ReseedSiteSettingsKey}={value}.");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine($"{EntraSetup.ReseedSiteSettingsKey}={value} written to Web.Server user secrets.".Green());
      if (Command.Clear)
      {
        Terminal.WriteLine("Later Development boots will keep persisted Entra policy (no overwrite).");
      }
      else
      {
        Terminal.WriteLine("The next Development boot of Web.Server will overwrite EntraSignInEnabled and EntraAllowBootstrap from configuration (PasskeyPromptMode is left unchanged).");
        Terminal.WriteLine("The flag is ignored outside Development.");
        Terminal.WriteLine("After confirming the reseed, run `dev entra reseed --clear` so later boots keep admin edits.");
      }

      Terminal.WriteLine("Blunt fallback that drops principals and passkeys: `dev db reset --yes`");
      return Value;
    }
  }
}
