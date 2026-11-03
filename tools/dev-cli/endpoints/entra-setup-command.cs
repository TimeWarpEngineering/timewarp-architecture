#region Purpose
// `dev entra setup`: find-or-create the local Entra app registration and write Web.Server user secrets.
#endregion

#region Design
// Thin Nuru shell: declares the route and options, then hands EntraSetupOptions to
// EntraSetupRunner (services/entra-setup-runner.cs), which owns the non-interactive flow,
// idempotency, secret-mint rules, and dry-run behavior.
#endregion

namespace DevCli.Commands;

[NuruRoute("setup", Description = "Find-or-create the Entra app registration and write Web.Server user secrets")]
[NuruRouteExample("entra setup", Description = "Create or reuse TimeWarp Architecture Dev and write user secrets")]
[NuruRouteExample("entra setup --dry-run", Description = "Print az and user-secrets invocations without running them")]
[NuruRouteExample("entra setup --tenant contoso.com", Description = "Select the tenant by default domain when more than one is visible")]
[NuruRouteExample("entra setup --public-origin https://arch.timewarp.work --new-secret")]
internal sealed class EntraSetupCommand : EntraGroup, ICommand<Unit>
{
  [Option("name", Description = "App registration display name (default: TimeWarp Architecture Dev, suffixed with tenant domain when known)")]
  public string? Name { get; set; }

  [Option("tenant", Description = "Tenant id, default domain, or display name (required when more than one tenant is visible)")]
  public string? Tenant { get; set; }

  [Option("public-origin", Description = "Public HTTPS origin; adds {origin}/signin-oidc to redirect URIs and user secrets")]
  public string? PublicOrigin { get; set; }

  [Option("redirect-uri", Description = "Additional redirect URI (repeatable)")]
  public string[] RedirectUri { get; set; } = [];

  [Option("new-secret", Description = "Rotate the client secret on the same app (tenant/app changes re-mint without this flag)")]
  public bool NewSecret { get; set; }

  [Option("dry-run", Description = "Print az and user-secrets invocations without running them")]
  public bool DryRun { get; set; }

  internal sealed class Handler : ICommandHandler<EntraSetupCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(EntraSetupCommand command, CancellationToken ct)
    {
      EntraSetupOptions options = new(
        command.Name,
        command.Tenant,
        command.PublicOrigin,
        command.RedirectUri,
        command.NewSecret,
        command.DryRun);
      await EntraSetupRunner.RunAsync(Terminal, options, ct).ConfigureAwait(false);
      return Unit.Value;
    }
  }
}
