#region Purpose
// `dev deploy [--target compose|kubernetes|aca]`: the operator's manual `aspire deploy` of one publish
// target, after a preflight and a confirmation. Never run by CI.
#endregion

#region Design
// Deploying is a deliberate operator action, never a side effect of a merge (task 070-006): no
// workflow step and no `dev workflow` mode calls this verb; CI stops at `dev publish` plus its
// production-safety suites. Thin wrapper over
//   aspire deploy --apphost <csproj> --environment Production [--non-interactive] -- --Publish:Target=<t>
// with the target defaulting to the AppHost's Publish:Target default (compose).
// Preflight (services/aspire-deploy-preflight.cs): Aspire CLI 13.6+; for kubernetes, Helm 4.2+ and a
// current kubectl context, which is printed — any context works, a local kind cluster included; this
// verb never creates a cluster. For aca (task 070-007), `az login` and the subscription, printed:
// Azure__SubscriptionId when set, else the az CLI's, passed to `aspire deploy` as Azure__SubscriptionId. Compose prints the container runtime Aspire will use
// (ASPIRE_CONTAINER_RUNTIME, else docker); the verb itself calls no container CLI.
// Confirmation: --yes deploys with `--non-interactive` (every deploy parameter must then already be
// set, e.g. in user secrets or Parameters__* env vars). Without --yes the plan is printed and the
// operator is asked; when stdin is not a terminal there is nobody to ask, so it refuses instead of
// assuming yes; any answer but y/yes cancels with nothing run. Without --yes Aspire also prompts for
// any missing parameter.
// Pure targets/arguments/parsing/text live in services/aspire-deploy.cs (dev-cli-tests).
#endregion

namespace DevCli.Commands;

[NuruRoute("deploy", Description = "Deploy the AppHost with `aspire deploy` to one publish target (operator-run, never CI). Asks for confirmation unless --yes")]
[NuruRouteExample("deploy", Description = "Preflight, show the plan, ask, then aspire deploy the default target (compose)")]
[NuruRouteExample("deploy --target kubernetes", Description = "Deploy the Helm chart to the current kubectl context (Helm 4.2+)")]
[NuruRouteExample("deploy --target aca", Description = "Provision Azure Container Apps + Flexible Server in the az CLI's subscription (az login first)")]
[NuruRouteExample("deploy --target compose --yes", Description = "Deploy without prompting (aspire deploy --non-interactive)")]
internal sealed class DeployCommand : ICommand<Unit>
{
  [Option("target", "t", Description = "Publish target: compose | kubernetes | aca (default: compose, the AppHost's Publish:Target default)")]
  public string? Target { get; set; }

  [Option("yes", "y", Description = "Confirm without prompting and run aspire deploy --non-interactive")]
  public bool Yes { get; set; }

  internal sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      DeployPreflight? preflight = await AspireDeployPreflight.RunAsync(Terminal, "dev deploy", command.Target, ct);
      if (preflight is null) return Unit.Value;

      foreach (string line in AspireDeploy.BuildDeployPlanLines(preflight.AppHostProject, preflight.Target, preflight.Detail))
      {
        Terminal.WriteLine(line);
      }

      if (!command.Yes)
      {
        if (Terminal.IsInputRedirected)
        {
          Terminal.WriteErrorLine(AspireDeploy.DeployConfirmationRefusal.Red());
          Environment.ExitCode = 1;
          return Unit.Value;
        }

        if (!Confirm())
        {
          Terminal.WriteErrorLine(AspireDeploy.DeployDeclined.Red());
          Environment.ExitCode = 1;
          return Unit.Value;
        }
      }

      ShellBuilder aspire = Shell.Builder("aspire")
        .WithArguments(AspireDeploy.BuildDeployArguments(preflight.AppHostProject, preflight.Target, nonInteractive: command.Yes))
        .WithWorkingDirectory(preflight.RepoRoot)
        .WithNoValidation();
      foreach ((string name, string value) in preflight.AspireEnvironment)
      {
        aspire = aspire.WithEnvironmentVariable(name, value);
      }

      CommandOutput deploy = await aspire.PassthroughAsync(ct);

      if (!deploy.Success)
      {
        Terminal.WriteErrorLine($"aspire deploy failed (exit {deploy.ExitCode}).".Red());
        Environment.ExitCode = deploy.ExitCode == 0 ? 1 : deploy.ExitCode;
        return Unit.Value;
      }

      Terminal.WriteLine($"\n{preflight.Target.Name} deploy complete. Remove it with `dev deprovision --target {preflight.Target.Name}`.".Green());
      return Unit.Value;
    }

    private bool Confirm()
    {
      Terminal.Write("Deploy? [y/N] ");
      string? answer = Terminal.ReadLine();
      return string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
    }
  }
}
