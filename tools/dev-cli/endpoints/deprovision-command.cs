#region Purpose
// `dev deprovision [--target compose|kubernetes] --yes`: the operator's manual `aspire destroy` of a
// deployment `dev deploy` recorded on this machine. Deletes the deployment's data. Never run by CI.
#endregion

#region Design
// Thin wrapper over
//   aspire destroy --apphost <csproj> --environment Production --non-interactive --yes -- --Publish:Target=<t>
// (task 070-006), in this order:
//   1. Preflight shared with `dev deploy` (Aspire CLI 13.6+; kubernetes: Helm 4.2+ and the printed
//      kubectl context).
//   2. The deployment record. `aspire destroy` only knows deployments recorded under
//      ~/.aspire/deployments by `aspire deploy` on THIS machine from THIS checkout; without one it
//      reports "nothing to destroy" and exits 0 while the stack keeps running. So with no record the
//      verb runs nothing, says so, prints the manual removal for the target (`<runtime> compose
//      down --volumes`, or `helm uninstall` plus deleting the postgres claim) and exits 1. It never
//      falls back to a destructive command itself.
//   3. Without --yes: print the recorded deployment and what destroying it deletes, exit 1.
//   4. aspire destroy. For kubernetes, print how to check the postgres claim is gone afterwards.
// Runtime neutrality: Compose teardown is Aspire's (honours ASPIRE_CONTAINER_RUNTIME); the runtime
// name only appears in the printed manual commands.
// Pure record lookup and text live in services/aspire-deploy.cs (dev-cli-tests).
#endregion

namespace DevCli.Commands;

[NuruRoute("deprovision", Description = "Destroy a deployment `dev deploy` recorded on this machine with `aspire destroy` (deletes its data; requires --yes; operator-run, never CI)")]
[NuruRouteExample("deprovision", Description = "Show the recorded compose deployment that would be destroyed, without acting")]
[NuruRouteExample("deprovision --target kubernetes --yes", Description = "Uninstall the recorded Helm release")]
internal sealed class DeprovisionCommand : ICommand<Unit>
{
  [Option("target", "t", Description = "Publish target: compose | kubernetes (default: compose, the AppHost's Publish:Target default)")]
  public string? Target { get; set; }

  [Option("yes", "y", Description = "Confirm: destroy the deployment and permanently delete its data (required)")]
  public bool Yes { get; set; }

  internal sealed class Handler : ICommandHandler<DeprovisionCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(DeprovisionCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      DeployPreflight? preflight = await AspireDeployPreflight.RunAsync(Terminal, "dev deprovision", command.Target, ct);
      if (preflight is null) return Unit.Value;
      Terminal.WriteLine(preflight.Detail);

      string statePath = AspireDeployPreflight.StatePath(preflight.AppHostProject);
      DeploymentRecord? record = await AspireDeployPreflight.ReadRecordAsync(statePath, preflight.Target, ct);
      if (record is null)
      {
        string runtime = AspireDeploy.ContainerRuntime(Environment.GetEnvironmentVariable(AspireDeploy.ContainerRuntimeVariable));
        foreach (string line in AspireDeploy.BuildNoRecordLines(preflight.Target, statePath, AspireDeploy.AppHostPath(preflight.AppHostProject), runtime))
        {
          Terminal.WriteLine(line);
        }

        Environment.ExitCode = 1;
        return Unit.Value;
      }

      if (!command.Yes)
      {
        foreach (string line in AspireDeploy.BuildDeprovisionRefusalLines(preflight.Target, record))
        {
          Terminal.WriteLine(line);
        }

        Environment.ExitCode = 1;
        return Unit.Value;
      }

      CommandOutput destroy = await Shell.Builder("aspire")
        .WithArguments(AspireDeploy.BuildDestroyArguments(preflight.AppHostProject, preflight.Target))
        .WithWorkingDirectory(preflight.RepoRoot)
        .WithNoValidation()
        .PassthroughAsync(ct);

      if (!destroy.Success)
      {
        Terminal.WriteErrorLine($"aspire destroy failed (exit {destroy.ExitCode}).".Red());
        Environment.ExitCode = destroy.ExitCode == 0 ? 1 : destroy.ExitCode;
        return Unit.Value;
      }

      foreach (string line in AspireDeploy.BuildPostDestroyLines(preflight.Target, record))
      {
        Terminal.WriteLine(line);
      }

      Terminal.WriteLine($"\n{preflight.Target.Name} deployment destroyed.".Green());
      return Unit.Value;
    }
  }
}
