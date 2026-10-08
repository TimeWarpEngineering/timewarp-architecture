#region Purpose
// `dev open [--target compose|kubernetes|aca] [--port <n>] [--no-browser]`: open the app a `dev deploy`
// made in the browser — forwarding the kind ingress controller when it has no external address.
#endregion

#region Design
// The verb is `open`, not `run`: `dev run` already starts the AppHost locally (task 287). Operator-run;
// nothing here deploys or changes the deployment. Per target:
//   - kubernetes: the shared preflight (PreflightScope.Open: the current kubectl context answers and,
//     for kind, the cluster exists; no aspire/helm/registry checks). Reads the ingress controller's
//     Service (default the kind recipe's ingress-nginx / ingress-nginx-controller; --controller-namespace /
//     --controller-service override). An external LoadBalancer address is opened directly; otherwise
//     `kubectl port-forward` runs in the foreground with a real TTY (TtyPassthroughAsync — it streams),
//     the browser opens once localhost:<port> accepts connections, and Ctrl+C stops the forward. The port
//     (default 8080) is checked free first so the refusal can suggest --port instead of kubectl's error.
//   - compose: http://localhost:<port> — --port, else INGRESS_PORT in the published .env, else the
//     ingress-port parameter resolved like dev deploy's parameters, else the AppHost's default. No
//     forwarding: the ingress publishes the host port itself.
//   - aca: https://<fqdn> of the ingress container app from `az containerapp show … --query
//     properties.configuration.ingress.fqdn`, in the preflight's subscription and the resource group
//     from --resource-group, Azure__ResourceGroup or the AppHost user secret Azure:ResourceGroup.
// Browser: the first opener on PATH from DeployOperate.BrowserLaunchers (WSL: wslview, then
// explorer.exe); none, or --no-browser, prints the URL. Pure decisions live in
// services/deploy-operate.cs (dev-cli-tests).
#endregion

namespace DevCli.Commands;

using System.Net;
using System.Net.Sockets;

[NuruRoute("open", Description = "Open the deployed app in the browser (kubernetes: port-forwards the ingress controller unless it has an external address; operator-run)")]
[NuruRouteExample("open --target kubernetes", Description = "Forward localhost:8080 to the kind ingress-nginx controller and open the browser (Ctrl+C stops the forward)")]
[NuruRouteExample("open --target kubernetes --port 9090", Description = "Forward a different local port")]
[NuruRouteExample("open", Description = "Open the compose deployment at http://localhost:<ingress-port>")]
[NuruRouteExample("open --target aca --no-browser", Description = "Print the ingress container app's https URL")]
internal sealed class OpenCommand : ICommand<Unit>
{
  [Option("target", "t", Description = "Deploy target: compose | kubernetes | aca (default: compose, the AppHost's Publish:Target default)")]
  public string? Target { get; set; }

  [Option("port", Description = "Local port: kubernetes forwards it (default 8080); compose overrides the ingress-port lookup")]
  public string? Port { get; set; }

  [Option("no-browser", Description = "Print the URL instead of opening a browser")]
  public bool NoBrowser { get; set; }

  [Option("controller-namespace", Description = "kubernetes: namespace of the ingress controller Service (default ingress-nginx)")]
  public string? ControllerNamespace { get; set; }

  [Option("controller-service", Description = "kubernetes: ingress controller Service name (default ingress-nginx-controller)")]
  public string? ControllerService { get; set; }

  [Option("resource-group", "g", Description = "aca: resource group (default: Azure__ResourceGroup, else the AppHost user secret Azure:ResourceGroup)")]
  public string? ResourceGroup { get; set; }

  internal sealed class Handler : ICommandHandler<OpenCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(OpenCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      DeployPreflight? preflight = await AspireDeployPreflight.RunAsync(Terminal, "dev open", command.Target, PreflightScope.Open, ct);
      if (preflight is null) return Unit.Value;
      Terminal.WriteLine(preflight.Detail);

      if (preflight.Target == AspireDeploy.Kubernetes)
      {
        await OpenKubernetesAsync(command, preflight, ct);
      }
      else if (preflight.Target == AspireDeploy.ContainerApps)
      {
        await OpenContainerAppsAsync(command, preflight, ct);
      }
      else
      {
        await OpenComposeAsync(command, preflight, ct);
      }

      return Unit.Value;
    }

    private async Task OpenKubernetesAsync(OpenCommand command, DeployPreflight preflight, CancellationToken ct)
    {
      (int? port, string? portError) = DeployOperate.ParsePort(command.Port, DeployOperate.DefaultForwardPort);
      if (port is null)
      {
        Fail(portError!);
        return;
      }

      string context = preflight.KubectlContext!;
      string controllerNamespace = string.IsNullOrWhiteSpace(command.ControllerNamespace) ? DeployOperate.IngressControllerNamespace : command.ControllerNamespace.Trim();
      string controllerService = string.IsNullOrWhiteSpace(command.ControllerService) ? DeployOperate.IngressControllerService : command.ControllerService.Trim();

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
        Fail(DeployOperate.ControllerServiceMissingMessage(context, controllerNamespace, controllerService, reason));
        return;
      }

      OpenPlan plan = DeployOperate.DecideKubernetesOpen(
        DeployOperate.ParseLoadBalancerAddress(service.Stdout), controllerNamespace, controllerService, port.Value);
      Terminal.WriteLine(plan.Detail);
      if (!plan.PortForward)
      {
        await OpenBrowserAsync(plan.Url, command.NoBrowser, ct);
        return;
      }

      if (!IsPortFree(port.Value))
      {
        Fail(DeployOperate.PortInUseMessage(port.Value));
        return;
      }

      string[] forwardArguments = DeployOperate.BuildPortForwardArguments(context, controllerNamespace, controllerService, port.Value);
      Terminal.WriteLine($"kubectl {string.Join(' ', forwardArguments)}");
      Task<CommandOutput> forward = Shell.Builder("kubectl")
        .WithArguments(forwardArguments)
        .WithNoValidation()
        .TtyPassthroughAsync(ct);

      if (await WaitForListenerAsync(port.Value, forward, ct))
      {
        await OpenBrowserAsync(plan.Url, command.NoBrowser, ct);
      }

      CommandOutput result;
      try
      {
        result = await forward;
      }
      catch (OperationCanceledException) when (ct.IsCancellationRequested)
      {
        return;
      }

      // Ctrl+C ends the forward with a signal exit code; that is the normal way out.
      if (!result.Success && !ct.IsCancellationRequested && result.ExitCode is not (130 or -1))
      {
        Fail($"kubectl port-forward failed (exit {result.ExitCode}).", result.ExitCode);
      }
    }

    private async Task OpenComposeAsync(OpenCommand command, DeployPreflight preflight, CancellationToken ct)
    {
      if (!string.IsNullOrWhiteSpace(command.Port) && DeployOperate.ParsePort(command.Port, DeployOperate.DefaultForwardPort).Error is { } portError)
      {
        Fail(portError);
        return;
      }

      string envFile = Path.Combine(preflight.RepoRoot, DeployOperate.PublishedOutputDirectory(preflight.Target), ".env");
      string? envValue = File.Exists(envFile) ? DeployOperate.ParseEnvValue(await File.ReadAllTextAsync(envFile, ct), DeployOperate.IngressPortVariable) : null;
      ResolvedDeployParameter? parameter = envValue is null && string.IsNullOrWhiteSpace(command.Port)
        ? await AspireDeployPreflight.ResolveParameterAsync(preflight.AppHostProject, DeployOperate.IngressPortParameter, ct)
        : null;
      (string port, string source) = DeployOperate.ChooseComposePort(command.Port, envValue, parameter);
      Terminal.WriteLine($"Ingress port: {port} ({source})");
      await OpenBrowserAsync($"http://localhost:{port}", command.NoBrowser, ct);
    }

    private async Task OpenContainerAppsAsync(OpenCommand command, DeployPreflight preflight, CancellationToken ct)
    {
      ResourceGroupChoice resourceGroup = DeployOperate.ChooseResourceGroup(
        command.ResourceGroup,
        Environment.GetEnvironmentVariable(AspireDeploy.AzureResourceGroupVariable),
        string.IsNullOrWhiteSpace(command.ResourceGroup) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AspireDeploy.AzureResourceGroupVariable))
          ? await AspireDeployPreflight.ReadUserSecretAsync(preflight.AppHostProject, DeployOperate.AzureResourceGroupConfigurationKey, ct)
          : null);
      if (resourceGroup.Name is null)
      {
        Fail(DeployOperate.NoResourceGroupMessage("dev open", preflight.AppHostProject));
        return;
      }

      Terminal.WriteLine(resourceGroup.Detail);
      string[] arguments = DeployOperate.BuildIngressFqdnArguments(resourceGroup.Name, preflight.AzureSubscriptionId!);
      CommandOutput? show = await AspireDeployPreflight.ProbeAsync("az", arguments, ct);
      string? fqdn = DeployOperate.ParseSingleValue(show?.Success == true, show?.Stdout ?? "");
      if (fqdn is null)
      {
        string reason = show?.TimedOut == true ? AspireDeploy.ProbeTimedOutMessage("az", arguments) : AspireDeploy.FirstLine(show?.Stderr ?? "", "no FQDN returned");
        Fail($"Could not read the {DeployOperate.IngressResourceName} container app's FQDN (`az {string.Join(' ', arguments)}`: {reason}). Is the app deployed to {resourceGroup.Name}?");
        return;
      }

      await OpenBrowserAsync($"https://{fqdn}", command.NoBrowser, ct);
    }

    private async Task OpenBrowserAsync(string url, bool noBrowser, CancellationToken ct)
    {
      Terminal.WriteLine($"App: {url}".Green());
      if (noBrowser)
      {
        return;
      }

      string? kernel = File.Exists("/proc/version") ? await File.ReadAllTextAsync("/proc/version", ct) : null;
      bool wsl = DeployOperate.IsWsl(Environment.GetEnvironmentVariable("WSL_DISTRO_NAME"), kernel);
      foreach (BrowserLauncher launcher in DeployOperate.BrowserLaunchers(OperatingSystem.IsWindows(), OperatingSystem.IsMacOS(), wsl))
      {
        if (PathResolver.ResolveExecutable(launcher.Executable) is null)
        {
          continue;
        }

        CommandOutput opened = await Shell.Builder(launcher.Executable)
          .WithArguments([.. launcher.Arguments, url])
          .WithTimeout(TimeSpan.FromSeconds(15))
          .WithNoValidation()
          .CaptureAsync(ct);

        // explorer.exe reports exit 1 even when it opened the URL.
        if (opened.Success || (launcher.Executable == "explorer.exe" && !opened.TimedOut))
        {
          return;
        }
      }

      Terminal.WriteLine(DeployOperate.NoBrowserMessage(url).Yellow());
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

    /// <summary>True once localhost:<paramref name="port"/> accepts a connection; false if the forward ended first or never listened.</summary>
    private static async Task<bool> WaitForListenerAsync(int port, Task forward, CancellationToken ct)
    {
      DateTime deadline = DateTime.UtcNow.AddSeconds(30);
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

    private void Fail(string message, int exitCode = 1)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = exitCode == 0 ? 1 : exitCode;
    }
  }
}
