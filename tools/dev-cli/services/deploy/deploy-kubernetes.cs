#region Purpose
// The kubernetes half of `dev open` / `dev deploy migrate`: the forward port, the ingress controller
// Service, and the `kubectl exec` arguments.
#endregion

#region Design
// Ingress port (task 311): no default lives here. The port is the AppHost's ingress-port parameter,
// committed in the Parameters section of its appsettings.json (the template's ingressPort port
// generator picks one per generated app) and resolved like dev deploy's parameters —
// env, user secret, appsettings.Production.json, appsettings.json. One number per app: the Compose host
// port, the `dev open --target kubernetes` forward port and the compose fallback. --port overrides it;
// with neither, `dev open` refuses (NoIngressPortMessage) rather than guess a shared port such as 8080.
#endregion

namespace DevCli.Services;

using System.Text.Json;

/// <summary>The kubernetes half of <c>dev open</c> / <c>dev deploy migrate</c>: forward port, ingress controller Service and <c>kubectl exec</c>.</summary>
internal static class DeployKubernetes
{
  internal const int IngressControllerPort = 80;
  internal const string IngressControllerNamespace = "ingress-nginx";
  internal const string IngressControllerService = "ingress-nginx-controller";

  /// <summary>
  /// <paramref name="value"/> as a TCP port; an error naming <paramref name="origin"/> (e.g. <c>--port</c>) when it is not one.
  /// Returns the port or the error, never both.
  /// </summary>
  internal static (int? Port, string? Error) ParsePort(string value, string origin) =>
    int.TryParse(value.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int port)
      && port is > 0 and <= 65535
      ? (port, null)
      : (null, $"{origin} '{value}' is not a TCP port (1-65535).");

  /// <summary>
  /// The kubernetes forward port: <c>--port</c>, else the resolved <c>ingress-port</c> parameter. Returns the port and where it
  /// came from, or (null, the error) when the chosen value is not a TCP port or neither is set.
  /// </summary>
  internal static (int? Port, string Detail) ChooseForwardPort(string? option, ResolvedDeployParameter? parameter, string appHostProject)
  {
    if (!string.IsNullOrWhiteSpace(option))
    {
      (int? port, string? error) = ParsePort(option, "--port");
      return (port, error ?? "--port");
    }

    if (parameter is null)
    {
      return (null, NoIngressPortMessage(appHostProject));
    }

    string source = $"{DeployOperate.IngressPortParameter} from the {parameter.Source}";
    (int? parameterPort, string? parameterError) = ParsePort(parameter.Value, source);
    return (parameterPort, parameterError ?? source);
  }

  /// <summary>Refusal when neither <c>--port</c> nor the AppHost's <c>ingress-port</c> parameter gives a port.</summary>
  internal static string NoIngressPortMessage(string appHostProject) =>
    $"No ingress port: the AppHost parameter {DeployOperate.IngressPortParameter} is not set. Commit it in the {AspireDeployParameters.ParametersSection} section of "
    + $"{Path.Combine(Path.GetDirectoryName(appHostProject) ?? "", AspireDeployParameters.AppSettingsFileName)}, set it per machine with "
    + $"`dotnet user-secrets set '{AspireDeployParameters.ParameterConfigurationKey(DeployOperate.IngressPortParameter)}' '<port>' --project '{appHostProject}'`, or pass --port <n>.";

  internal static string PortInUseMessage(int port) =>
    $"Port {port} on localhost is already in use, so kubectl cannot forward to it. Pick a free one: dev open --target kubernetes --port <n>";

  /// <summary>Reads the ingress controller's Service, to see whether it has an external (LoadBalancer) address.</summary>
  internal static string[] BuildControllerServiceArguments(string context, string controllerNamespace, string controllerService) =>
    ["--context", context, "get", "service", controllerService, "--namespace", controllerNamespace, "--output", "json"];

  /// <summary>Forwards <paramref name="port"/> on localhost to the controller's port 80, in the foreground.</summary>
  internal static string[] BuildPortForwardArguments(string context, string controllerNamespace, string controllerService, int port) =>
    ["--context", context, "port-forward", "--namespace", controllerNamespace, $"service/{controllerService}", $"{port}:{IngressControllerPort}"];

  /// <summary>
  /// The external address of a <c>LoadBalancer</c> Service from <c>kubectl get service -o json</c> (the first
  /// <c>status.loadBalancer.ingress</c> hostname or ip); null for any other type, a pending address or unparseable JSON.
  /// </summary>
  internal static string? ParseLoadBalancerAddress(string serviceJson)
  {
    try
    {
      using var document = JsonDocument.Parse(serviceJson);
      JsonElement root = document.RootElement;
      if (Child(root, "spec") is not { } spec
        || Child(spec, "type") is not { ValueKind: JsonValueKind.String } type
        || type.GetString() != "LoadBalancer"
        || Child(Child(Child(root, "status"), "loadBalancer"), "ingress") is not { ValueKind: JsonValueKind.Array } ingress)
      {
        return null;
      }

      foreach (JsonElement entry in ingress.EnumerateArray())
      {
        foreach (string key in (string[])["hostname", "ip"])
        {
          if (Child(entry, key) is { ValueKind: JsonValueKind.String } value && value.GetString() is { Length: > 0 } address)
          {
            return address;
          }
        }
      }

      return null;
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static JsonElement? Child(JsonElement? element, string name) =>
    element is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty(name, out JsonElement child) ? child : null;

  /// <summary>An external controller address is opened directly; otherwise the verb forwards <paramref name="port"/>.</summary>
  internal static OpenPlan DecideKubernetesOpen(string? loadBalancerAddress, string controllerNamespace, string controllerService, int port) =>
    loadBalancerAddress is not null
      ? new OpenPlan(
        $"http://{(loadBalancerAddress.Contains(':', StringComparison.Ordinal) ? $"[{loadBalancerAddress}]" : loadBalancerAddress)}",
        PortForward: false,
        $"service/{controllerService} ({controllerNamespace}) has an external address: {loadBalancerAddress} — no port-forward")
      : new OpenPlan(
        $"http://localhost:{port}",
        PortForward: true,
        $"service/{controllerService} ({controllerNamespace}) has no external address — forwarding localhost:{port} → :{IngressControllerPort} (Ctrl+C stops it)");

  /// <summary>Refusal when the controller Service cannot be read: the recipe's install command, or the override options.</summary>
  internal static string ControllerServiceMissingMessage(string context, string controllerNamespace, string controllerService, string reason) =>
    $"Cannot read the ingress controller Service {controllerService} in namespace {controllerNamespace} (context {context}: {reason}). "
    + "Install ingress-nginx (step 4 of the kind recipe in the tw-deploy skill), or name your controller: "
    + "dev open --target kubernetes --controller-namespace <namespace> --controller-service <service>";

  /// <summary>
  /// <c>kubectl exec</c> of <paramref name="psqlCommand"/> (<see cref="DeployMigrationHistory.PsqlScriptCommand"/> or <see cref="DeployMigrationHistory.PsqlHistoryCommand"/>)
  /// in the postgres StatefulSet of <paramref name="kubernetesNamespace"/>, stdin attached for the script.
  /// </summary>
  internal static string[] BuildKubectlMigrateArguments(string context, string kubernetesNamespace, string psqlCommand) =>
    ["--context", context, "exec", "-i", "--namespace", kubernetesNamespace, DeployOperate.PostgresStatefulSet, "--", "sh", "-c", psqlCommand];

  internal static string ForwardNotReadyMessage(string url, int seconds) =>
    $"The port-forward is not listening after {seconds}s; the browser was not opened. Open {url} once it is (Ctrl+C stops the forward).";
}
