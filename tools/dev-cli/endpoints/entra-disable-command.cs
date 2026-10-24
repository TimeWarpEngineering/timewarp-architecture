#region Purpose
// `dev entra disable`: drop Microsoft365 from SignInOptions:Methods in Web.Server user secrets.
#endregion

#region Design
// Local only — no Azure change. Idempotent. Passkey stays listed. The app registration and other
// Entra secrets stay so `dev entra setup` without --new-secret can add Microsoft365 again without
// minting a credential. dotnet user-secrets remove fails when the key is absent, so remove runs
// only when the list shows the key.
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
      if (secrets.ContainsKey(EntraSetup.Microsoft365MethodKey))
      {
        CommandOutput removed = await Cli
          .RemoveUserSecretAsync(EntraSetup.Microsoft365MethodKey)
          .ConfigureAwait(false);
        if (!removed.Success)
        {
          Cli.WriteFailure(removed, "Failed to remove Microsoft365 from SignInOptions:Methods.");
          Environment.ExitCode = 1;
          return Unit.Value;
        }
      }

      CommandOutput passkey = await Cli
        .SetUserSecretAsync(EntraSetup.PasskeyMethodKey, "Passkey", maskValue: false)
        .ConfigureAwait(false);
      if (!passkey.Success)
      {
        Cli.WriteFailure(passkey, "Failed to keep Passkey in SignInOptions:Methods.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Terminal.WriteLine("SignInOptions:Methods keeps Passkey. Microsoft365 was removed.".Green());
      Terminal.WriteLine("Azure app registration was not changed. Add Microsoft365 again with `dev entra setup`.");
      return Unit.Value;
    }
  }
}
