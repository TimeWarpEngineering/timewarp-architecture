#region Purpose
// `dev deploy [--target compose|kubernetes|aca|aca-neon]`: one operator command from a checkout to a
// live, migrated deployment. Never run by CI.
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
//   4. smoke the two anonymous paths (/web-server/alive, /api/identity/entra/offered), then print
//      the URL. Smoke retries a cold start. It never requests /api/health.
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
// Smoke's URL is the same one `dev open` uses: compose is http://localhost:<ingress-port>, ACA is
// https://<ingress fqdn>, kubernetes is the LoadBalancer address or a temporary kubectl port-forward
// that is cancelled after the checks. `dev open --target kubernetes` is what keeps a forward.
// This command is the DeployGroup's empty route, so `dev deploy` runs it and `dev deploy --help`
// lists it beside `migrate` (a separate `deploy` route would be shadowed by the group's help table).
// Pure step text lives in services/deploy-pipeline.cs (dev-cli-tests). The HTTP calls live in
// services/deploy-smoke.cs and are not compiled into the test project.
#endregion

namespace DevCli.Commands;

using System.Net;
using System.Net.Sockets;

[NuruRoute("", Description = "Publish, deploy, migrate when the app has a database, smoke the ingress, and print the URL (operator-run, never CI). Asks unless --yes")]
[NuruRouteExample("deploy", Description = "One command for the default target (compose): publish, deploy, migrate, smoke")]
[NuruRouteExample("deploy --target kubernetes", Description = "One command: publish, deploy the Helm chart to the current kubectl context, migrate, smoke")]
[NuruRouteExample("deploy --target aca", Description = "One command: publish, provision Azure Container Apps + Flexible Server, migrate, smoke the ingress (az login first)")]
[NuruRouteExample("deploy --target aca-neon", Description = "One command: publish, provision Azure Container Apps wired to Neon, migrate, smoke. Connection strings stay off the command line")]
[NuruRouteExample("deploy --target compose --yes", Description = "Run the whole path without prompting (aspire deploy --non-interactive)")]
internal sealed class DeployCommand : DeployGroup, ICommand<Unit>
{
  private const int ListenerTimeoutSeconds = 30;

  [Option("target", "t", Description = "Publish target: compose | kubernetes | aca | aca-neon (default: compose, the AppHost's Publish:Target default)")]
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

      DeployPreflight? preflight = await AspireDeployPreflight.RunAsync(Terminal, "dev deploy", command.Target, PreflightScope.Deploy, ct);
      if (preflight is null) return Unit.Value;

      bool hasDatabase = DeployOperate.FindSourceMigrationFolders(preflight.AppHostProject, preflight.RepoRoot).Length > 0;
      foreach (string line in AspireDeploy.BuildDeployPlanLines(
        preflight.AppHostProject, preflight.Target, preflight.Detail, preflight.Parameters, preflight.Secrets ?? []))
      {
        Terminal.WriteLine(line);
      }

      Terminal.WriteLine("Steps: " + string.Join(" → ", DeployPipeline.Steps(hasDatabase)));
      string? passkeyHost = PasskeyHost(preflight);
      if (passkeyHost is not null)
      {
        Terminal.WriteLine($"Passkey host: {passkeyHost} (Parameters:{AspireDeploy.PasskeyRpHostParameter})");
      }

      if (!command.Yes)
      {
        if (Terminal.IsInputRedirected)
        {
          Terminal.WriteErrorLine(AspireDeploy.DeployConfirmationRefusal.Red());
          Environment.ExitCode = 1;
          return Unit.Value;
        }

        if (!Confirm(hasDatabase))
        {
          Terminal.WriteErrorLine(AspireDeploy.DeployDeclined.Red());
          Environment.ExitCode = 1;
          return Unit.Value;
        }
      }

      int pipelineExit = await DeployPipeline.RunAsync(hasDatabase, RunStepAsync, ct);
      if (pipelineExit != 0 && Environment.ExitCode == 0)
      {
        Environment.ExitCode = pipelineExit;
      }

      return Unit.Value;

      async Task<int> RunStepAsync(string step, CancellationToken stepCancellation)
      {
        switch (step)
        {
          case DeployPipeline.Publish:
          {
            Terminal.WriteLine(DeployPipeline.StepBanner(DeployPipeline.Publish, preflight.Target));
            int publishExit = await AspirePublish.PublishFilesAsync(
              Terminal, preflight.RepoRoot, preflight.Target, stepCancellation, PublishEnvironment(preflight),
              warnWhenOlder: false);
            if (publishExit != 0)
            {
              Stop(DeployPipeline.FailureMessage(DeployPipeline.Publish, preflight.Target, publishExit), publishExit);
              return Environment.ExitCode;
            }

            return 0;
          }
          case DeployPipeline.Deploy:
          {
            Terminal.WriteLine(DeployPipeline.StepBanner(DeployPipeline.Deploy, preflight.Target));
            ShellBuilder aspire = Shell.Builder("aspire")
              .WithArguments(AspireDeploy.BuildDeployArguments(
                preflight.AppHostProject, preflight.Target, nonInteractive: command.Yes, preflight.Parameters, preflight.Secrets ?? []))
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
              Stop(DeployPipeline.FailureMessage(DeployPipeline.Deploy, preflight.Target, deployExit), deployExit);
              return Environment.ExitCode;
            }

            if (!hasDatabase)
            {
              Terminal.WriteLine(DeployPipeline.SkipMigrateMessage(preflight.Target).Yellow());
            }

            return 0;
          }
          case DeployPipeline.Migrate:
          {
            Terminal.WriteLine(DeployPipeline.StepBanner(DeployPipeline.Migrate, preflight.Target));
            await new DeployMigrateCommand.Handler(Terminal).Handle(
              new DeployMigrateCommand { Target = preflight.Target.Name, Yes = true }, stepCancellation);
            if (Environment.ExitCode != 0)
            {
              Stop(
                DeployPipeline.FailureMessage(DeployPipeline.Migrate, preflight.Target, Environment.ExitCode),
                Environment.ExitCode);
              return Environment.ExitCode;
            }

            return 0;
          }
          case DeployPipeline.Smoke:
          {
            Terminal.WriteLine(DeployPipeline.StepBanner(DeployPipeline.Smoke, preflight.Target));
            int smokeExit = await SmokeAsync(preflight, stepCancellation);
            if (smokeExit != 0)
            {
              Stop(DeployPipeline.FailureMessage(DeployPipeline.Smoke, preflight.Target, smokeExit), smokeExit);
              return Environment.ExitCode;
            }

            return 0;
          }
          default:
          {
            Stop(DeployPipeline.FailureMessage(step, preflight.Target, 1), 1);
            return Environment.ExitCode;
          }
        }
      }
    }

    private bool Confirm(bool hasDatabase)
    {
      Terminal.Write(DeployPipeline.ConfirmPrompt(hasDatabase));
      string? answer = Terminal.ReadLine();
      return string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<int> SmokeAsync(DeployPreflight preflight, CancellationToken ct)
    {
      if (AspireDeploy.IsAzureContainerApps(preflight.Target))
      {
        return await SmokeContainerAppsAsync(preflight, ct);
      }

      if (preflight.Target == AspireDeploy.Kubernetes)
      {
        return await SmokeKubernetesAsync(preflight, ct);
      }

      return await SmokeComposeAsync(preflight, ct);
    }

    private async Task<int> SmokeComposeAsync(DeployPreflight preflight, CancellationToken ct)
    {
      string envFile = Path.Combine(preflight.RepoRoot, DeployOperate.PublishedOutputDirectory(preflight.Target), ".env");
      string? envValue = File.Exists(envFile)
        ? DeployOperate.ParseEnvValue(await File.ReadAllTextAsync(envFile, ct), DeployOperate.IngressPortVariable)
        : null;
      ResolvedDeployParameter? parameter = envValue is null
        ? await AspireDeployPreflight.ResolveParameterAsync(preflight.AppHostProject, DeployOperate.IngressPortParameter, ct)
        : null;
      (string? port, string? source) = DeployOperate.ChooseComposePort(null, envValue, parameter);
      if (port is null)
      {
        Terminal.WriteErrorLine(DeployOperate.NoIngressPortMessage(preflight.AppHostProject).Red());
        return 1;
      }

      if (DeployOperate.ParsePort(port, source!).Error is { } portError)
      {
        Terminal.WriteErrorLine(portError.Red());
        return 1;
      }

      string url = $"http://localhost:{port}";
      Terminal.WriteLine($"Ingress port: {port} ({source})");
      int exit = await SmokeAtAsync(url, ct);
      if (exit == 0)
      {
        Terminal.WriteLine(DeployPipeline.CompleteMessage(preflight.Target, url).Green());
      }

      return exit;
    }

    private async Task<int> SmokeContainerAppsAsync(DeployPreflight preflight, CancellationToken ct)
    {
      if (string.IsNullOrWhiteSpace(preflight.AzureSubscriptionId))
      {
        Terminal.WriteErrorLine("dev deploy: smoke needs the Azure subscription the deploy used. The deployment is up.".Red());
        return 1;
      }

      string? processGroup = Environment.GetEnvironmentVariable(AspireDeploy.AzureResourceGroupVariable);
      string? copiedGroup = preflight.AspireEnvironment.GetValueOrDefault(AspireDeploy.AzureResourceGroupVariable);
      string? environmentGroup = string.IsNullOrWhiteSpace(processGroup) ? copiedGroup : processGroup;
      string? secret = string.IsNullOrWhiteSpace(environmentGroup)
        ? await AspireDeployPreflight.ReadUserSecretAsync(preflight.AppHostProject, DeployOperate.AzureResourceGroupConfigurationKey, ct)
        : null;
      ResourceGroupChoice resourceGroup = DeployOperate.ChooseResourceGroup(null, environmentGroup, secret);
      if (resourceGroup.Name is null)
      {
        Terminal.WriteErrorLine(DeployPipeline.SmokeNeedsResourceGroup(preflight.Target).Red());
        return 1;
      }

      Terminal.WriteLine(resourceGroup.Detail);
      string[] arguments = DeployOperate.BuildIngressFqdnArguments(resourceGroup.Name, preflight.AzureSubscriptionId);
      CommandOutput? show = await AspireDeployPreflight.ProbeAsync("az", arguments, ct);
      string? fqdn = DeployOperate.ParseSingleValue(show?.Success == true, show?.Stdout ?? "");
      if (fqdn is null)
      {
        string reason = show?.TimedOut == true
          ? AspireDeploy.ProbeTimedOutMessage("az", arguments)
          : AspireDeploy.FirstLine(show?.Stderr ?? "", "no FQDN returned");
        Terminal.WriteErrorLine($"Could not read the {DeployOperate.IngressResourceName} container app's FQDN (`az {string.Join(' ', arguments)}`: {reason}). The deployment is up.".Red());
        return 1;
      }

      string url = $"https://{fqdn}";
      int exit = await SmokeAtAsync(url, ct);
      if (exit == 0)
      {
        Terminal.WriteLine(DeployPipeline.CompleteMessage(preflight.Target, url).Green());
      }

      return exit;
    }

    private async Task<int> SmokeKubernetesAsync(DeployPreflight preflight, CancellationToken ct)
    {
      if (string.IsNullOrWhiteSpace(preflight.KubectlContext))
      {
        Terminal.WriteErrorLine("dev deploy: smoke needs the kubectl context the deploy used. The deployment is up.".Red());
        return 1;
      }

      ResolvedDeployParameter? parameter = await AspireDeployPreflight.ResolveParameterAsync(
        preflight.AppHostProject, DeployOperate.IngressPortParameter, ct);
      (int? port, string portDetail) = DeployOperate.ChooseForwardPort(null, parameter, preflight.AppHostProject);
      if (port is null)
      {
        Terminal.WriteErrorLine(portDetail.Red());
        return 1;
      }

      string context = preflight.KubectlContext;
      const string controllerNamespace = DeployOperate.IngressControllerNamespace;
      const string controllerService = DeployOperate.IngressControllerService;
      CommandOutput? service = await AspireDeployPreflight.ProbeAsync(
        "kubectl", DeployOperate.BuildControllerServiceArguments(context, controllerNamespace, controllerService), ct);
      if (service is not { Success: true })
      {
        string reason = service switch
        {
          null => "kubectl is not on PATH",
          { TimedOut: true } => $"timed out after {AspireDeploy.ProbeTimeout.TotalSeconds:0}s",
          _ => AspireDeploy.FirstLine(service.Stderr, $"exit {service.ExitCode}"),
        };
        Terminal.WriteErrorLine(DeployOperate.ControllerServiceMissingMessage(context, controllerNamespace, controllerService, reason).Red());
        return 1;
      }

      OpenPlan plan = DeployOperate.DecideKubernetesOpen(
        DeployOperate.ParseLoadBalancerAddress(service.Stdout), controllerNamespace, controllerService, port.Value);
      Terminal.WriteLine(plan.Detail);
      if (!plan.PortForward)
      {
        int direct = await SmokeAtAsync(plan.Url, ct);
        if (direct == 0)
        {
          Terminal.WriteLine(DeployPipeline.CompleteMessage(preflight.Target, plan.Url).Green());
        }

        return direct;
      }

      if (!IsPortFree(port.Value))
      {
        Terminal.WriteErrorLine(DeployOperate.PortInUseMessage(port.Value).Red());
        return 1;
      }

      string[] forwardArguments = DeployOperate.BuildPortForwardArguments(context, controllerNamespace, controllerService, port.Value);
      Terminal.WriteLine($"kubectl {string.Join(' ', forwardArguments)}");
      using var forwardCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
      Task<CommandOutput> forward = Shell.Builder("kubectl")
        .WithArguments(forwardArguments)
        .WithNoValidation()
        .CaptureAsync(forwardCancellation.Token);
      try
      {
        if (!await WaitForListenerAsync(port.Value, forward, ct))
        {
          Terminal.WriteErrorLine($"dev deploy: the temporary port-forward for {preflight.Target.Name} was not listening after {ListenerTimeoutSeconds}s. The deployment is up.".Red());
          return 1;
        }

        int exit = await SmokeAtAsync(plan.Url, ct);
        if (exit == 0)
        {
          Terminal.WriteLine(DeployPipeline.CompleteMessage(preflight.Target, plan.Url).Green());
        }

        return exit;
      }
      finally
      {
        await forwardCancellation.CancelAsync();
        try
        {
          await forward;
        }
        catch (OperationCanceledException)
        {
          // Cancelling the token is how the temporary forward is stopped.
        }

        Terminal.WriteLine(DeployPipeline.TemporaryForwardStopped(preflight.Target).Yellow());
      }
    }

    private async Task<int> SmokeAtAsync(string url, CancellationToken ct)
    {
      DeploySmokeResult result = await DeploySmoke.CheckAsync(url, ct);
      foreach (string line in result.Lines)
      {
        Terminal.WriteLine(line);
      }

      return result.Ok ? 0 : 1;
    }

    /// <summary>The extra passkey host when one is set and it is not localhost. A hostname, so it may be printed.</summary>
    private static string? PasskeyHost(DeployPreflight preflight)
    {
      string variable = AspireDeploy.ParameterEnvironmentVariable(AspireDeploy.PasskeyRpHostParameter);
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
      string variable = AspireDeploy.ParameterEnvironmentVariable(AspireDeploy.PasskeyRpHostParameter);
      return preflight.AspireEnvironment.TryGetValue(variable, out string? value)
        ? new Dictionary<string, string> { [variable] = value }
        : null;
    }

    private static bool IsPortFree(int port)
    {
      try
      {
        using TcpListener listener = new(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true;
      }
      catch (SocketException)
      {
        return false;
      }
    }

    /// <summary>True once localhost:<paramref name="port"/> accepts a connection.</summary>
    private static async Task<bool> WaitForListenerAsync(int port, Task forward, CancellationToken ct)
    {
      DateTime deadline = DateTime.UtcNow.AddSeconds(ListenerTimeoutSeconds);
      while (DateTime.UtcNow < deadline && !forward.IsCompleted && !ct.IsCancellationRequested)
      {
        try
        {
          using TcpClient client = new();
          await client.ConnectAsync(IPAddress.Loopback, port, ct);
          return true;
        }
        catch (SocketException)
        {
          await Task.Delay(250, ct).ContinueWith(_ => { }, TaskScheduler.Default);
        }
        catch (OperationCanceledException)
        {
          return false;
        }
      }

      return false;
    }

    private void Stop(string message, int exitCode)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = exitCode == 0 ? 1 : exitCode;
    }
  }
}
