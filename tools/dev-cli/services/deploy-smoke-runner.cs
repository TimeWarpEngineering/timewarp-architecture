#region Purpose
// The smoke step of `dev deploy`: find the deployed ingress URL per target, smoke it, and open it.
#endregion

#region Design
// Process half of the smoke step (the pure step text is services/deploy-pipeline.cs, tested by
// dev-cli-tests; the HTTP calls are services/deploy-smoke.cs). The URL is the one `dev open` uses:
// compose is http://localhost:<ingress-port>, ACA is https://<ingress fqdn>, kubernetes with an
// external address is that address. Kubernetes without an external address smokes through a
// temporary kubectl port-forward that is cancelled after the checks, prints the
// `dev open --target kubernetes` hint, and does not keep a foreground forward. Smoke retries a cold
// start. It never requests /api/health. After a passing smoke the URL is opened through
// DeployBrowser, the same launcher as `dev open`; --no-browser prints the URL and opens nothing, and
// no terminal, or no opener on PATH, prints the URL and is not a failure. The port-forward and the az
// and kubectl probes are wrappers of the real tools (Amuru); nothing reconstructs their internals.
#endregion

namespace DevCli.Services;

using System.Net;
using System.Net.Sockets;

/// <summary>Smokes a finished <c>dev deploy</c> through the target's ingress URL and opens it in the browser.</summary>
internal static class DeploySmokeRunner
{
  private const int ListenerTimeoutSeconds = 30;

  internal static async Task<int> SmokeAsync(ITerminal terminal, DeployPreflight preflight, bool noBrowser, CancellationToken ct)
  {
    if (AspireDeploy.IsAzureContainerApps(preflight.Target))
    {
      return await SmokeContainerAppsAsync(terminal, preflight, noBrowser, ct);
    }

    if (preflight.Target == AspireDeploy.Kubernetes)
    {
      return await SmokeKubernetesAsync(terminal, preflight, noBrowser, ct);
    }

    return await SmokeComposeAsync(terminal, preflight, noBrowser, ct);
  }

  private static async Task<int> SmokeComposeAsync(ITerminal terminal, DeployPreflight preflight, bool noBrowser, CancellationToken ct)
  {
    string envFile = Path.Combine(preflight.RepoRoot, DeployOperate.PublishedOutputDirectory(preflight.Target), ".env");
    string? envValue = File.Exists(envFile)
      ? DeployCompose.ParseEnvValue(await File.ReadAllTextAsync(envFile, ct), DeployOperate.IngressPortVariable)
      : null;
    ResolvedDeployParameter? parameter = envValue is null
      ? await AspireDeployPreflight.ResolveParameterAsync(preflight.AppHostProject, DeployOperate.IngressPortParameter, ct)
      : null;
    (string? port, string? source) = DeployCompose.ChooseComposePort(null, envValue, parameter);
    if (port is null)
    {
      terminal.WriteErrorLine(DeployKubernetes.NoIngressPortMessage(preflight.AppHostProject).Red());
      return 1;
    }

    if (DeployKubernetes.ParsePort(port, source!).Error is { } portError)
    {
      terminal.WriteErrorLine(portError.Red());
      return 1;
    }

    string url = $"http://localhost:{port}";
    terminal.WriteLine($"Ingress port: {port} ({source})");
    int exit = await SmokeAtAsync(terminal, url, ct);
    if (exit == 0)
    {
      await AnnounceAsync(terminal, preflight.Target, url, noBrowser, ct);
    }

    return exit;
  }

  private static async Task<int> SmokeContainerAppsAsync(ITerminal terminal, DeployPreflight preflight, bool noBrowser, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(preflight.AzureSubscriptionId))
    {
      terminal.WriteErrorLine("dev deploy: smoke needs the Azure subscription the deploy used. The deployment is up.".Red());
      return 1;
    }

    string? processGroup = Environment.GetEnvironmentVariable(AspireDeployAzure.AzureResourceGroupVariable);
    string? copiedGroup = preflight.AspireEnvironment.GetValueOrDefault(AspireDeployAzure.AzureResourceGroupVariable);
    string? environmentGroup = string.IsNullOrWhiteSpace(processGroup) ? copiedGroup : processGroup;
    string? secret = string.IsNullOrWhiteSpace(environmentGroup)
      ? await AspireDeployPreflight.ReadUserSecretAsync(preflight.AppHostProject, DeployAca.AzureResourceGroupConfigurationKey, ct)
      : null;
    ResourceGroupChoice resourceGroup = DeployAca.ChooseResourceGroup(null, environmentGroup, secret);
    if (resourceGroup.Name is null)
    {
      terminal.WriteErrorLine(DeployPipeline.SmokeNeedsResourceGroup(preflight.Target).Red());
      return 1;
    }

    terminal.WriteLine(resourceGroup.Detail);
    string[] arguments = DeployAca.BuildIngressFqdnArguments(resourceGroup.Name, preflight.AzureSubscriptionId);
    CommandOutput? show = await AspireDeployPreflight.ProbeAsync("az", arguments, ct);
    string? fqdn = DeployAca.ParseSingleValue(show?.Success == true, show?.Stdout ?? "");
    if (fqdn is null)
    {
      string reason = show?.TimedOut == true
        ? AspireDeployCluster.ProbeTimedOutMessage("az", arguments)
        : AspireDeployCluster.FirstLine(show?.Stderr ?? "", "no FQDN returned");
      terminal.WriteErrorLine($"Could not read the {DeployOperate.IngressResourceName} container app's FQDN (`az {string.Join(' ', arguments)}`: {reason}). The deployment is up.".Red());
      return 1;
    }

    string url = $"https://{fqdn}";
    int exit = await SmokeAtAsync(terminal, url, ct);
    if (exit == 0)
    {
      await AnnounceAsync(terminal, preflight.Target, url, noBrowser, ct);
    }

    return exit;
  }

  private static async Task<int> SmokeKubernetesAsync(ITerminal terminal, DeployPreflight preflight, bool noBrowser, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(preflight.KubectlContext))
    {
      terminal.WriteErrorLine("dev deploy: smoke needs the kubectl context the deploy used. The deployment is up.".Red());
      return 1;
    }

    ResolvedDeployParameter? parameter = await AspireDeployPreflight.ResolveParameterAsync(
      preflight.AppHostProject, DeployOperate.IngressPortParameter, ct);
    (int? port, string portDetail) = DeployKubernetes.ChooseForwardPort(null, parameter, preflight.AppHostProject);
    if (port is null)
    {
      terminal.WriteErrorLine(portDetail.Red());
      return 1;
    }

    string context = preflight.KubectlContext;
    const string controllerNamespace = DeployKubernetes.IngressControllerNamespace;
    const string controllerService = DeployKubernetes.IngressControllerService;
    CommandOutput? service = await AspireDeployPreflight.ProbeAsync(
      "kubectl", DeployKubernetes.BuildControllerServiceArguments(context, controllerNamespace, controllerService), ct);
    if (service is not { Success: true })
    {
      string reason = service switch
      {
        null => "kubectl is not on PATH",
        { TimedOut: true } => $"timed out after {AspireDeployCluster.ProbeTimeout.TotalSeconds:0}s",
        _ => AspireDeployCluster.FirstLine(service.Stderr, $"exit {service.ExitCode}"),
      };
      terminal.WriteErrorLine(DeployKubernetes.ControllerServiceMissingMessage(context, controllerNamespace, controllerService, reason).Red());
      return 1;
    }

    OpenPlan plan = DeployKubernetes.DecideKubernetesOpen(
      DeployKubernetes.ParseLoadBalancerAddress(service.Stdout), controllerNamespace, controllerService, port.Value);
    terminal.WriteLine(plan.Detail);
    if (!plan.PortForward)
    {
      int direct = await SmokeAtAsync(terminal, plan.Url, ct);
      if (direct == 0)
      {
        await AnnounceAsync(terminal, preflight.Target, plan.Url, noBrowser, ct);
      }

      return direct;
    }

    if (!IsPortFree(port.Value))
    {
      terminal.WriteErrorLine(DeployKubernetes.PortInUseMessage(port.Value).Red());
      return 1;
    }

    string[] forwardArguments = DeployKubernetes.BuildPortForwardArguments(context, controllerNamespace, controllerService, port.Value);
    terminal.WriteLine($"kubectl {string.Join(' ', forwardArguments)}");
    using var forwardCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Task<CommandOutput> forward = Shell.Builder("kubectl")
      .WithArguments(forwardArguments)
      .WithNoValidation()
      .CaptureAsync(forwardCancellation.Token);
    try
    {
      if (!await WaitForListenerAsync(port.Value, forward, ct))
      {
        terminal.WriteErrorLine($"dev deploy: the temporary port-forward for {preflight.Target.Name} was not listening after {ListenerTimeoutSeconds}s. The deployment is up.".Red());
        return 1;
      }

      int exit = await SmokeAtAsync(terminal, plan.Url, ct);
      if (exit == 0)
      {
        string summary = DeployPipeline.CompleteMessage(preflight.Target, plan.Url);
        terminal.WriteLine(summary.Green());
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

      // No foreground forward. The hint names `dev open`, which is what keeps one.
      terminal.WriteLine(DeployPipeline.TemporaryForwardStopped(preflight.Target).Yellow());
    }
  }

  /// <summary>
  /// Prints the smoke summary and, when <see cref="SmokeFollowUp.OpenBrowser"/> is set, opens the URL
  /// through <see cref="DeployBrowser"/>. A temporary kubernetes forward does not call this method.
  /// </summary>
  private static async Task AnnounceAsync(
    ITerminal terminal,
    DeployTarget target,
    string url,
    bool noBrowser,
    CancellationToken ct)
  {
    SmokeFollowUp followUp = DeployPipeline.FollowUpAfterSmoke(target, url, noBrowser);
    terminal.WriteLine(followUp.Summary.Green());
    if (followUp.OpenBrowser)
    {
      await DeployBrowser.OpenAsync(terminal, url, noBrowser: false, ct);
    }
  }

  private static async Task<int> SmokeAtAsync(ITerminal terminal, string url, CancellationToken ct)
  {
    DeploySmokeResult result = await DeploySmoke.CheckAsync(url, ct);
    foreach (string line in result.Lines)
    {
      terminal.WriteLine(line);
    }

    return result.Ok ? 0 : 1;
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
}
