#region Purpose
// `dev entra disable`: drop Microsoft365 from SignInOptions:Methods in Web.Server user secrets.
#endregion

#region Design
// Local only — no Azure change. Idempotent. Removes every SignInOptions:Methods user secret whose
// value is Microsoft365 and does not write Passkey. Other method entries stay. The app registration
// and other Entra secrets stay so `dev entra setup` without --new-secret can add Microsoft365 again
// without minting a credential. dotnet user-secrets remove fails when the key is absent, so remove
// runs only for keys the list shows.
#endregion

namespace DevCli.Commands;

[NuruRoute("disable", Description = "Remove Microsoft365 from SignInOptions:Methods in Web.Server user secrets (no Azure change)")]
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

    public async Task<Unit> Handle(EntraDisableCommand command, CancellationToken ct)
    {
      if (!EntraCli.TryFindRepoRoot(Terminal, out string repoRoot))
      {
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Cli = new EntraCli(Terminal, repoRoot, dryRun: false, ct);
      CommandOutput listed = await Cli.ListUserSecretsAsync().ConfigureAwait(false);
      if (!listed.Success)
      {
        Cli.WriteFailure(listed, "Failed to list Web.Server user secrets.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Dictionary<string, string> secrets = EntraSetup.ParseUserSecretsList(listed.Stdout);
      IReadOnlyList<string> methodKeys = EntraSetup.Microsoft365MethodKeysToRemove(secrets);
      foreach (string methodKey in methodKeys)
      {
        CommandOutput removed = await Cli
          .RemoveUserSecretAsync(methodKey)
          .ConfigureAwait(false);
        if (!removed.Success)
        {
          Cli.WriteFailure(removed, $"Failed to remove {methodKey} from SignInOptions:Methods.");
          Environment.ExitCode = 1;
          return Unit.Value;
        }
      }

      if (methodKeys.Count == 0)
      {
        Terminal.WriteLine("SignInOptions:Methods in user secrets does not list Microsoft365.".Green());
      }
      else
      {
        Terminal.WriteLine("Microsoft365 was removed from SignInOptions:Methods in user secrets.".Green());
      }

      Terminal.WriteLine("Azure app registration was not changed. Add Microsoft365 again with `dev entra setup`.");
      return Unit.Value;
    }
  }
}
