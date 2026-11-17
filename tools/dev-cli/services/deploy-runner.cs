#region Purpose
// The orchestration behind `dev deploy`: preflight, plan, confirmation, then the publish, deploy, migrate and
// smoke steps, stopping at the first non-zero exit.
#endregion

#region Design
// Deploying is a deliberate operator action, never a side effect of a merge (task 070-006): no
// workflow step and no `dev workflow` mode calls this verb; CI stops at `dev publish` plus its
// production-safety suites. Task 393 makes this the pit-of-success path. It runs
// DeployPipeline.RunAsync over DeployPipeline.Steps, stopping at the first non-zero exit
// and naming the verb that retries that step:
//   1. aspire publish into artifacts/aspire-output/<target> (files only; the safety suite stays on
//      `dev publish`) so the migrations bundle exists before anything is deployed.
//   2. aspire deploy (the same arguments and preflight as before).
//   3. migrate, only when platform/postgres/migrations sits beside the AppHost. Otherwise the step
//      is skipped and the skip line says so. The inner verb is `dev deploy migrate` with --yes,
//      because this confirmation already covered the pipeline. There is no --no-migrate flag.
//   4. smoke the two anonymous paths (/web-server/alive, /api/identity/sign-in-methods), then open
//      the ingress URL through DeployBrowser, the same launcher as `dev open` (DeploySmokeRunner).
//      --no-browser prints the URL and opens nothing. --yes does not change that.
// `dev publish`, `dev deploy migrate`, and `dev open` stay the one-step verbs.
// Preflight (services/aspire-deploy-preflight.cs): Aspire CLI 13.6+ (refused below that; a CLI older
// than Directory.Packages.props <AspireVersion> warns once, and the publish step does not repeat it);
// for kubernetes, Helm 4.2+, a
// current kubectl context whose API answers, which is printed — any context works, a local kind cluster
// included; for a kind context the kind cluster exists and the registry at registry-endpoint answers;
// this verb never creates a cluster. Deploy parameters (tasks 286, 288): every value-less AppHost parameter of
// the target (kubernetes: k8s-namespace, helm-release-name, registry-endpoint, registry-repository) must
// resolve — a Parameters__<name> env var, else the AppHost user secret Parameters:<name>, else the
// AppHost appsettings Parameters section, where the template commits all four — and is forwarded as
// `--Parameters:<name>=<value>` after `--`; missing ones are refused with the command to set each
// (forwarded values are printed and appear in argv, so they are never secrets). Compose prints the container runtime Aspire will use
// (ASPIRE_CONTAINER_RUNTIME, else docker); the verb itself calls no container CLI. For aca (task
// 070-007), `az login` and the subscription with its source, printed: the Azure__SubscriptionId
// environment variable, else the AppHost user secret Azure:SubscriptionId, else the az CLI's — only
// that fallback, and a user-secret subscription, are passed to `aspire deploy` as Azure__SubscriptionId.
// An environment value is inherited and not rewritten, so the az CLI never overrides a subscription
// the operator pinned. Production does not load AppHost user secrets, which is why the copy exists.
// Azure:Location and Azure:ResourceGroup are copied the same way when unset.
// aca-neon (task 317) uses the same Azure preflight and the same ACA environment. The Neon
// connection strings are secret parameters. A value found in appsettings is refused. They are not
// printed and not placed on the aspire command line. A user-secret value is copied into the aspire
// process environment as Parameters__<name> because Production does not load user secrets.
// The passkey host (Parameters:passkey-rp-host) is a hostname, not a secret. A user-secret value is
// copied the same way and handed to aspire publish and aspire deploy. A value that is not a bare DNS
// name refuses the deploy. aca-neon warns (and continues) when the Neon host's AWS region is far from
// Azure:Location. The warning does not print the connection string.
// Confirmation: --yes deploys with `--non-interactive`. Without --yes the plan (with the forwarded
// parameters) and the step list are printed and the operator is asked; when stdin is not a terminal
// there is nobody to ask, so it refuses instead of assuming yes; any answer but y/yes cancels with
// nothing run. aspire deploy runs with TTY passthrough (it inherits the terminal), so any prompt
// Aspire still shows — e.g. an optional parameter, or aca's location and resource group — works
// interactively; a piped stdin would make Aspire fail "non-interactive mode" instead of asking.
// The migrate step is handed in by the endpoint (migrate), so this service does not depend on a command
// class. Pure step text lives in services/deploy-pipeline.cs (dev-cli-tests). The smoke step is
// services/deploy-smoke-runner.cs; its HTTP calls live in services/deploy-smoke.cs and are not compiled
// into the test project.
#endregion

namespace DevCli.Services;

/// <summary>Runs the <c>dev deploy</c> path: preflight, plan, confirmation, then publish, deploy, migrate and smoke.</summary>
internal static class DeployRunner
{
  /// <summary>
  /// Runs <c>dev deploy</c> for <paramref name="target"/> (null = the default). <paramref name="migrate"/> runs
  /// <c>dev deploy migrate</c> for a target name with <c>--yes</c>; it reports failure through <see cref="Environment.ExitCode"/>.
  /// </summary>
  internal static async Task RunAsync(
    ITerminal terminal,
    string? target,
    bool yes,
    bool noBrowser,
    Func<string, CancellationToken, Task> migrate,
    CancellationToken ct)
  {
    Environment.ExitCode = 0;

    DeployPreflight? preflight = await AspireDeployPreflight.RunAsync(terminal, "dev deploy", target, PreflightScope.Deploy, ct);
    if (preflight is null) return;

    bool hasDatabase = DeployStaleScript.FindSourceMigrationFolders(preflight.AppHostProject, preflight.RepoRoot).Length > 0;
    foreach (string line in AspireDeployText.BuildDeployPlanLines(
      preflight.AppHostProject, preflight.Target, preflight.Detail, preflight.Parameters, preflight.Secrets ?? []))
    {
      terminal.WriteLine(line);
    }

    terminal.WriteLine("Steps: " + string.Join(" → ", DeployPipeline.Steps(hasDatabase)));
    string? passkeyHost = PasskeyHost(preflight);
    if (passkeyHost is not null)
    {
      terminal.WriteLine($"Passkey host: {passkeyHost} (Parameters:{AspireDeployParameters.PasskeyRpHostParameter})");
    }

    if (!yes)
    {
      if (terminal.IsInputRedirected)
      {
        terminal.WriteErrorLine(AspireDeployText.DeployConfirmationRefusal.Red());
        Environment.ExitCode = 1;
        return;
      }

      if (!Confirm(terminal, hasDatabase, noBrowser))
      {
        terminal.WriteErrorLine(AspireDeployText.DeployDeclined.Red());
        Environment.ExitCode = 1;
        return;
      }
    }

    int pipelineExit = await DeployPipeline.RunAsync(hasDatabase, RunStepAsync, ct);
    if (pipelineExit != 0 && Environment.ExitCode == 0)
    {
      Environment.ExitCode = pipelineExit;
    }

    return;

    async Task<int> RunStepAsync(string step, CancellationToken stepCancellation)
    {
      switch (step)
      {
        case DeployPipeline.Publish:
        {
          terminal.WriteLine(DeployPipeline.StepBanner(DeployPipeline.Publish, preflight.Target));
          int publishExit = await AspirePublish.PublishFilesAsync(
            terminal, preflight.RepoRoot, preflight.Target, stepCancellation, PublishEnvironment(preflight),
            warnWhenOlder: false);
          if (publishExit != 0)
          {
            Stop(terminal, DeployPipeline.FailureMessage(DeployPipeline.Publish, preflight.Target, publishExit), publishExit);
            return Environment.ExitCode;
          }

          return 0;
        }
        case DeployPipeline.Deploy:
        {
          terminal.WriteLine(DeployPipeline.StepBanner(DeployPipeline.Deploy, preflight.Target));
          ShellBuilder aspire = Shell.Builder("aspire")
            .WithArguments(AspireDeploy.BuildDeployArguments(
              preflight.AppHostProject, preflight.Target, nonInteractive: yes, preflight.Parameters, preflight.Secrets ?? []))
            .WithWorkingDirectory(preflight.RepoRoot)
            .WithNoValidation();
          foreach ((string name, string value) in preflight.AspireEnvironment)
          {
            aspire = aspire.WithEnvironmentVariable(name, value);
          }

          CommandOutput deploy = await aspire.TtyPassthroughAsync(stepCancellation);
          if (!deploy.Success)
          {
            int deployExit = deploy.ExitCode == 0 ? 1 : deploy.ExitCode;
            Stop(terminal, DeployPipeline.FailureMessage(DeployPipeline.Deploy, preflight.Target, deployExit), deployExit);
            return Environment.ExitCode;
          }

          if (!hasDatabase)
          {
            terminal.WriteLine(DeployPipeline.SkipMigrateMessage(preflight.Target).Yellow());
          }

          return 0;
        }
        case DeployPipeline.Migrate:
        {
          terminal.WriteLine(DeployPipeline.StepBanner(DeployPipeline.Migrate, preflight.Target));
          await migrate(preflight.Target.Name, stepCancellation);
          if (Environment.ExitCode != 0)
          {
            Stop(terminal,
              DeployPipeline.FailureMessage(DeployPipeline.Migrate, preflight.Target, Environment.ExitCode),
              Environment.ExitCode);
            return Environment.ExitCode;
          }

          return 0;
        }
        case DeployPipeline.Smoke:
        {
          terminal.WriteLine(DeployPipeline.StepBanner(DeployPipeline.Smoke, preflight.Target));
          int smokeExit = await DeploySmokeRunner.SmokeAsync(terminal, preflight, noBrowser, stepCancellation);
          if (smokeExit != 0)
          {
            Stop(terminal, DeployPipeline.FailureMessage(DeployPipeline.Smoke, preflight.Target, smokeExit), smokeExit);
            return Environment.ExitCode;
          }

          return 0;
        }
        default:
        {
          Stop(terminal, DeployPipeline.FailureMessage(step, preflight.Target, 1), 1);
          return Environment.ExitCode;
        }
      }
    }
  }

  private static bool Confirm(ITerminal terminal, bool hasDatabase, bool noBrowser)
  {
    terminal.Write(DeployPipeline.ConfirmPrompt(hasDatabase, noBrowser));
    string? answer = terminal.ReadLine();
    return string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
      || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>The extra passkey host when one is set and it is not localhost. A hostname, so it may be printed.</summary>
  private static string? PasskeyHost(DeployPreflight preflight)
  {
    string variable = AspireDeployParameters.ParameterEnvironmentVariable(AspireDeployParameters.PasskeyRpHostParameter);
    string? value = Environment.GetEnvironmentVariable(variable)
      ?? preflight.AspireEnvironment.GetValueOrDefault(variable);
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    string host = value.Trim();
    return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ? null : host;
  }

  /// <summary>
  /// Environment for the file publish. The passkey host only: secret values stay on the deploy process
  /// and are not written into the publish directory.
  /// </summary>
  private static Dictionary<string, string>? PublishEnvironment(DeployPreflight preflight)
  {
    string variable = AspireDeployParameters.ParameterEnvironmentVariable(AspireDeployParameters.PasskeyRpHostParameter);
    return preflight.AspireEnvironment.TryGetValue(variable, out string? value)
      ? new Dictionary<string, string> { [variable] = value }
      : null;
  }

  private static void Stop(ITerminal terminal, string message, int exitCode)
  {
    terminal.WriteErrorLine(message.Red());
    Environment.ExitCode = exitCode == 0 ? 1 : exitCode;
  }
}
