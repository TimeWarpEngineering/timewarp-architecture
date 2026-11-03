#region Purpose
// `dev deploy [--target compose|kubernetes|aca|aca-neon]`: one operator command from a checkout to a
// live, migrated deployment. Never run by CI.
#endregion

#region Design
// Thin endpoint: the route, the options, and one call. The orchestration (preflight, plan,
// confirmation, publish, deploy, migrate, smoke) is services/deploy-runner.cs (DeployRunner), and the
// smoke step is services/deploy-smoke-runner.cs; their Design regions carry the behavior. Deploying is a
// deliberate operator action, never a side effect of a merge (task 070-006): no workflow step and no
// `dev workflow` mode calls this verb. This command is the DeployGroup's empty route, so `dev deploy`
// runs it and `dev deploy --help` lists it beside `migrate` (a separate `deploy` route would be
// shadowed by the group's help table). The migrate step is `dev deploy migrate --yes`, run through
// DeployMigrateCommand.Handler and handed to the runner as a delegate.
#endregion

namespace DevCli.Commands;

[NuruRoute("", Description = "Publish, deploy, migrate when the app has a database, smoke the ingress, and open it in the browser (operator-run, never CI). --no-browser prints the URL. Asks unless --yes")]
[NuruRouteExample("deploy", Description = "One command for the default target (compose): publish, deploy, migrate, smoke")]
[NuruRouteExample("deploy --target kubernetes", Description = "One command: publish, deploy the Helm chart to the current kubectl context, migrate, smoke")]
[NuruRouteExample("deploy --target aca", Description = "One command: publish, provision Azure Container Apps + Flexible Server, migrate, smoke the ingress (az login first)")]
[NuruRouteExample("deploy --target aca-neon", Description = "One command: publish, provision Azure Container Apps wired to Neon, migrate, smoke. Connection strings stay off the command line")]
[NuruRouteExample("deploy --target compose --yes", Description = "Run the whole path without prompting (aspire deploy --non-interactive)")]
[NuruRouteExample("deploy --no-browser", Description = "Run the path and print the URL without opening a browser")]
internal sealed class DeployCommand : DeployGroup, ICommand<Unit>
{
  [Option("target", "t", Description = "Publish target: compose | kubernetes | aca | aca-neon (default: compose, the AppHost's Publish:Target default)")]
  public string? Target { get; set; }

  [Option("yes", "y", Description = "Confirm without prompting and run aspire deploy --non-interactive")]
  public bool Yes { get; set; }

  [Option("no-browser", Description = "Print the URL instead of opening a browser")]
  public bool NoBrowser { get; set; }

  internal sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      await DeployRunner.RunAsync(
        Terminal,
        command.Target,
        command.Yes,
        command.NoBrowser,
        (target, token) => new DeployMigrateCommand.Handler(Terminal).Handle(
          new DeployMigrateCommand { Target = target, Yes = true }, token),
        ct);
      return Unit.Value;
    }
  }
}
