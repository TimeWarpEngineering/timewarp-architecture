#region Purpose
// `dev deprovision [--target compose|kubernetes] [--yes]`: the operator's manual `aspire destroy` of a
// deployment `dev deploy` made. Deletes the deployment's data. Never run by CI.
#endregion

#region Design
// Thin wrapper over
//   aspire destroy --apphost <csproj> --environment Production [--yes --non-interactive] -- --Publish:Target=<t>
// (task 070-006), in this order:
//   1. Preflight shared with `dev deploy` (Aspire CLI 13.6+; kubernetes: Helm 4.2+ and the printed
//      kubectl context, so the operator sees what they are about to hit).
//   2. Confirmation is Aspire's: without --yes `aspire destroy` asks itself; with no terminal and no
//      --yes there is nobody to ask, so the verb refuses (as `dev deploy` does) instead of assuming yes.
//   3. aspire destroy. On failure print the manual removal for the target and exit with Aspire's exit
//      code: `aspire destroy` only knows deployments recorded on the machine that deployed. The verb
//      never runs a destructive fallback itself and never inspects Aspire's deployment state.
//   4. On success for kubernetes, print how to check the postgres claim is gone.
// Runtime neutrality: Compose teardown is Aspire's (honours ASPIRE_CONTAINER_RUNTIME); the runtime
// name only appears in the printed manual commands.
// Pure argument and text helpers live in services/aspire-deploy.cs (dev-cli-tests).
#endregion

namespace DevCli.Commands;

[NuruRoute("deprovision", Description = "Destroy a deployment with `aspire destroy` (deletes its data; Aspire asks unless --yes; operator-run, never CI)")]
[NuruRouteExample("deprovision", Description = "Preflight, then aspire destroy the compose deployment (Aspire asks for confirmation)")]
[NuruRouteExample("deprovision --target kubernetes --yes", Description = "Uninstall the Helm release without prompting")]
internal sealed class DeprovisionCommand : ICommand<Unit>
{
  [Option("target", "t", Description = "Publish target: compose | kubernetes (default: compose, the AppHost's Publish:Target default)")]
  public string? Target { get; set; }

  [Option("yes", "y", Description = "Confirm without prompting and run aspire destroy --yes --non-interactive")]
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

      if (!command.Yes && Terminal.IsInputRedirected)
      {
        Terminal.WriteErrorLine(AspireDeploy.DestroyConfirmationRefusal.Red());
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      CommandOutput destroy = await Shell.Builder("aspire")
        .WithArguments(AspireDeploy.BuildDestroyArguments(preflight.AppHostProject, preflight.Target, command.Yes))
        .WithWorkingDirectory(preflight.RepoRoot)
        .WithNoValidation()
        .PassthroughAsync(ct);

      if (!destroy.Success)
      {
        Terminal.WriteErrorLine($"aspire destroy failed (exit {destroy.ExitCode}).".Red());
        string runtime = AspireDeploy.ContainerRuntime(Environment.GetEnvironmentVariable(AspireDeploy.ContainerRuntimeVariable));
        foreach (string line in AspireDeploy.BuildManualCleanupLines(preflight.Target, runtime))
        {
          Terminal.WriteLine(line);
        }

        Environment.ExitCode = destroy.ExitCode == 0 ? 1 : destroy.ExitCode;
        return Unit.Value;
      }

      foreach (string line in AspireDeploy.BuildPostDestroyLines(preflight.Target))
      {
        Terminal.WriteLine(line);
      }

      Terminal.WriteLine($"\n{preflight.Target.Name} deployment destroyed.".Green());
      return Unit.Value;
    }
  }
}
