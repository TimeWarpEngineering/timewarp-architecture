#region Purpose
// Targets' names, argument builders, parsing, decisions and operator text for `dev open` and
// `dev deploy migrate` (reach and migrate a deployment `dev deploy` made), per deploy target.
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests Compile-include them and gate both
// verbs without a cluster, a container runtime or Azure (task 287). The process half is the two
// command handlers plus services/aspire-deploy-preflight.cs (shared with `dev deploy`).
//
// Wrap the tool, never its internals (070-006 / 284): every value comes from the deploy configuration
// (Parameters:* resolved exactly as `dev deploy` resolves them — AspireDeploy.ResolveParameters), the
// published output under artifacts/aspire-output/<target>, or a documented CLI (kubectl, az, the
// container runtime's `compose ls`). The resource names below are the AppHost's (constants.cs:
// YarpResourceName, PostgresResourceName, PostgresDatabaseResourceName) and Aspire's published names
// derived from them (statefulset/postgres-statefulset); dev-cli-tests reads constants.cs and the
// ingress-port default in program.cs so the copies cannot drift.
//
// Missing published output: `dev deploy migrate` REFUSES with the exact `dev publish <target>`
// command instead of publishing itself. Publishing is a separate, gated step (it wipes the output
// directory and runs the production-safety suite) and the operator should see which script runs —
// a silent publish could migrate with a script from a different checkout state than the deploy.
//
// Compose project: `aspire deploy` chooses the Compose project name itself (the published
// docker-compose.yaml has no `name:`), so the verb never guesses it. It asks the runtime —
// `<runtime> compose ls --format json`, the running projects — and uses `--project-name` when the
// operator passes one; otherwise the one project whose compose file lives in this checkout, else the
// only running project; anything else is a refusal listing the candidates. The project's own compose
// file(s) from `compose ls` are passed with --file so exec resolves the postgres service.
//
// psql: PGPASSWORD comes from the postgres container's own environment (the image enforces password
// auth even on the in-container socket, and a piped session cannot prompt), so the password never
// passes through the dev CLI. The script is piped on stdin (`exec -T` / `exec -i`), so no TTY.
//
// ACA: the bundle runs with --connection through a temporary `operator-migrate` firewall rule for the
// operator's IP; RunAcaBundleAsync creates the rule inside try and deletes it in finally, so it is
// deleted on a failed create, a failed bundle and an exception alike, and a failed delete is reported
// with the exact command to run by hand — from inside the finally, so even when an exception (Ctrl+C)
// propagates. The connection string is the Key Vault secret Aspire
// provisioned (connectionstrings--postgres-db in the postgres-kv vault); it is passed to the bundle
// only and never printed (RedactConnection). It is an argument because --connection is the bundle's
// documented interface; it is visible in the operator's own process list for the bundle's lifetime,
// accepted for an operator-run verb on the operator's machine. The operator's IP is --client-ip, else what
// https://api.ipify.org reports.
#endregion

namespace DevCli.Services;

using System.Net;
using System.Text.Json;

/// <summary>Where <c>dev open</c> sends the browser, and whether it must keep a kubectl port-forward running for it.</summary>
internal sealed record OpenPlan(string Url, bool PortForward, string Detail);

/// <summary>A running Compose project as <c>compose ls --format json</c> reports it.</summary>
internal sealed record ComposeProject(string Name, string Status, IReadOnlyList<string> ConfigFiles);

/// <summary>The Compose project to exec into, or why there is none.</summary>
internal sealed record ComposeProjectChoice(ComposeProject? Project, string? Refusal);

/// <summary>The resource group an aca verb targets and the line that says where it came from; null Name when unset.</summary>
internal sealed record ResourceGroupChoice(string? Name, string Detail);

/// <summary>One process the aca migration runs: executable, arguments, and whether it needs the terminal.</summary>
internal sealed record ProcessStep(string Executable, IReadOnlyList<string> Arguments, bool Terminal = false);

/// <summary>Outcome of the aca bundle run: the exit code to report, and the firewall-rule delete's own exit code.</summary>
internal sealed record AcaMigrationResult(int ExitCode, int CleanupExitCode, string? CleanupFailure);

/// <summary>A browser opener candidate: executable plus the arguments before the URL.</summary>
internal sealed record BrowserLauncher(string Executable, IReadOnlyList<string> Arguments);

/// <summary>Builds and decides the <c>dev open</c> / <c>dev deploy migrate</c> invocations per deploy target.</summary>
internal static class DeployOperate
{
  internal const int DefaultForwardPort = 8080;
  internal const int IngressControllerPort = 80;
  internal const string IngressControllerNamespace = "ingress-nginx";
  internal const string IngressControllerService = "ingress-nginx-controller";

  // AppHost constants.cs: YarpResourceName, PostgresResourceName, PostgresDatabaseResourceName (dev-cli-tests agree).
  internal const string IngressResourceName = "ingress";
  internal const string PostgresResourceName = "postgres";
  internal const string PostgresDatabaseName = "postgres-db";
  internal const string PostgresStatefulSet = "statefulset/postgres-statefulset";
  internal const string IngressPortParameter = "ingress-port";
  internal const string IngressPortVariable = "INGRESS_PORT";
  internal const string DefaultIngressPort = "8080";

  internal const string MigrationScript = "efmigrations/web-migrations.sql";
  internal const string MigrationBundle = "efmigrations/web-migrations";
  internal const string FirewallRuleName = "operator-migrate";
  internal const string ConnectionStringSecret = "connectionstrings--postgres-db";
  internal const string AzureResourceGroupConfigurationKey = "Azure:ResourceGroup";
  internal static readonly Uri ClientIpProbe = new("https://api.ipify.org");

  /// <summary>The psql invocation inside the postgres container; the password comes from the container's environment.</summary>
  internal const string PsqlCommand =
    "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d " + PostgresDatabaseName + " -v ON_ERROR_STOP=1";

  /// <summary>Where <c>dev publish &lt;target&gt;</c> writes the target's output, relative to the repository root.</summary>
  internal static string PublishedOutputDirectory(DeployTarget target) => $"artifacts/aspire-output/{target.Name}";

  /// <summary>The published file <c>dev deploy migrate</c> runs for <paramref name="target"/>: the bundle for aca, else the SQL script.</summary>
  internal static string MigrationArtifact(DeployTarget target) =>
    target == AspireDeploy.ContainerApps ? MigrationBundle : MigrationScript;

  /// <summary>Refusal when the published migration artifact is missing: the exact command that writes it.</summary>
  internal static string MissingPublishedOutputMessage(DeployTarget target, string path) =>
    $"No published migrations for {target.Name}: {path} does not exist. Publish them first (the verb never publishes for you), then re-run: "
    + $"dev publish {target.Name}";

  /// <summary>
  /// The port <c>--port</c> asks for (null = <paramref name="fallback"/>); an error when it is not a TCP port. Returns the
  /// port or the error, never both.
  /// </summary>
  internal static (int? Port, string? Error) ParsePort(string? value, int fallback)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return (fallback, null);
    }

    return int.TryParse(value.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int port)
      && port is > 0 and <= 65535
      ? (port, null)
      : (null, $"--port '{value}' is not a TCP port (1-65535).");
  }

  internal static string PortInUseMessage(int port) =>
    $"Port {port} on localhost is already in use, so kubectl cannot forward to it. Pick a free one: dev open --target kubernetes --port <n>";

  // ── kubernetes ─────────────────────────────────────────────────────────────

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
  /// <c>kubectl exec</c> of psql in the postgres StatefulSet of <paramref name="kubernetesNamespace"/>, stdin attached for the
  /// script.
  /// </summary>
  internal static string[] BuildKubectlMigrateArguments(string context, string kubernetesNamespace) =>
    ["--context", context, "exec", "-i", "--namespace", kubernetesNamespace, PostgresStatefulSet, "--", "sh", "-c", PsqlCommand];

  // ── compose ────────────────────────────────────────────────────────────────

  /// <summary>The value of <paramref name="name"/> in a <c>.env</c> file; null when absent or blank (the published .env leaves parameters blank).</summary>
  internal static string? ParseEnvValue(string envText, string name)
  {
    foreach (string raw in envText.Split('\n'))
    {
      string line = raw.Trim();
      if (line.StartsWith('#'))
      {
        continue;
      }

      int separator = line.IndexOf('=', StringComparison.Ordinal);
      if (separator > 0 && line[..separator].Trim() == name)
      {
        string value = line[(separator + 1)..].Trim().Trim('"', '\'');
        return value.Length > 0 ? value : null;
      }
    }

    return null;
  }

  /// <summary>
  /// The Compose host port: <c>--port</c>, else <c>INGRESS_PORT</c> in the published <c>.env</c>, else the resolved
  /// <c>ingress-port</c> parameter, else the AppHost's default (8080). Returns the port and where it came from.
  /// </summary>
  internal static (string Port, string Source) ChooseComposePort(string? option, string? envValue, ResolvedDeployParameter? parameter) =>
    !string.IsNullOrWhiteSpace(option) ? (option.Trim(), "--port")
    : envValue is not null ? (envValue, $"{IngressPortVariable} in the published .env")
    : parameter is not null ? (parameter.Value, $"{IngressPortParameter} from the {parameter.Source}")
    : (DefaultIngressPort, $"the AppHost's {IngressPortParameter} default");

  /// <summary><c>&lt;runtime&gt; compose ls --format json</c>: the running Compose projects.</summary>
  internal static string[] BuildComposeListArguments() => ["compose", "ls", "--format", "json"];

  /// <summary>Parses <c>compose ls --format json</c> (Name, Status, ConfigFiles comma-separated); empty when unparseable.</summary>
  internal static ComposeProject[] ParseComposeProjects(string json)
  {
    try
    {
      using var document = JsonDocument.Parse(json);
      if (document.RootElement.ValueKind != JsonValueKind.Array)
      {
        return [];
      }

      return
      [
        .. document.RootElement.EnumerateArray()
          .Where(entry => entry.ValueKind == JsonValueKind.Object)
          .Select(entry => new ComposeProject(
            Text(entry, "Name"),
            Text(entry, "Status"),
            [.. Text(entry, "ConfigFiles").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]))
          .Where(project => project.Name.Length > 0),
      ];
    }
    catch (JsonException)
    {
      return [];
    }

    static string Text(JsonElement entry, string name) =>
      entry.EnumerateObject().FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).Value is
        { ValueKind: JsonValueKind.String } value
        ? value.GetString() ?? ""
        : "";
  }

  /// <summary>
  /// The project to migrate: <paramref name="requested"/> when given (it must be running), else the one whose compose file
  /// is under <paramref name="repoRoot"/>, else the only running project; otherwise a refusal naming the candidates.
  /// </summary>
  internal static ComposeProjectChoice ChooseComposeProject(IReadOnlyList<ComposeProject> projects, string? requested, string repoRoot, string runtime)
  {
    if (!string.IsNullOrWhiteSpace(requested))
    {
      ComposeProject? named = projects.FirstOrDefault(project => string.Equals(project.Name, requested.Trim(), StringComparison.Ordinal));
      return named is not null
        ? new ComposeProjectChoice(named, null)
        : new ComposeProjectChoice(null, $"No running Compose project named '{requested.Trim()}'. {ComposeCandidates(projects, runtime)}");
    }

    string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repoRoot)) + Path.DirectorySeparatorChar;
    ComposeProject[] inCheckout =
    [
      .. projects.Where(project => project.ConfigFiles.Any(file => Path.GetFullPath(file).StartsWith(root, StringComparison.Ordinal))),
    ];
    if (inCheckout.Length == 1)
    {
      return new ComposeProjectChoice(inCheckout[0], null);
    }

    if (inCheckout.Length == 0 && projects.Count == 1)
    {
      return new ComposeProjectChoice(projects[0], null);
    }

    return projects.Count == 0
      ? new ComposeProjectChoice(null, $"No Compose project is running (`{runtime} compose ls` lists none). Deploy first: dev deploy --target compose")
      : new ComposeProjectChoice(null, $"Several Compose projects are running; name the deployment: dev deploy migrate --target compose --project-name <name>. {ComposeCandidates(projects, runtime)}");
  }

  private static string ComposeCandidates(IReadOnlyList<ComposeProject> projects, string runtime) =>
    projects.Count == 0
      ? $"`{runtime} compose ls` lists no running project."
      : $"Running (`{runtime} compose ls`): {string.Join(", ", projects.Select(project => project.Name))}.";

  /// <summary><c>&lt;runtime&gt; compose … exec -T postgres</c> psql for <paramref name="project"/>, stdin attached for the script.</summary>
  /// <summary>The migrate command as an operator would type it in pwsh: the script piped on stdin (<c>Get-Content -Raw</c>).</summary>
  internal static string BuildPipedCommandDisplay(string script, string executable, IReadOnlyList<string> arguments) =>
    $"Get-Content -Raw {PwshQuote(script)} | {executable} {string.Join(' ', arguments.Select(PwshQuote))}";

  /// <summary>pwsh single-quoted literal (embedded ' doubled) when the argument needs quoting; as-is otherwise.</summary>
  internal static string PwshQuote(string argument) =>
    argument.Length > 0 && argument.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '/' or ':' or '=')
      ? argument
      : $"'{argument.Replace("'", "''", StringComparison.Ordinal)}'";

  internal static string[] BuildComposeMigrateArguments(ComposeProject project) =>
  [
    "compose", "--project-name", project.Name,
    .. project.ConfigFiles.SelectMany(file => (string[])["--file", file]),
    "exec", "-T", PostgresResourceName, "sh", "-c", PsqlCommand,
  ];

  // ── aca ────────────────────────────────────────────────────────────────────

  /// <summary>
  /// The resource group, in the order Aspire reads it plus the verbs' option: <c>--resource-group</c>, else
  /// <c>Azure__ResourceGroup</c>, else the AppHost user secret <c>Azure:ResourceGroup</c>.
  /// </summary>
  internal static ResourceGroupChoice ChooseResourceGroup(string? option, string? environmentValue, string? userSecretValue) =>
    !string.IsNullOrWhiteSpace(option) ? new ResourceGroupChoice(option.Trim(), $"Resource group: {option.Trim()} from --resource-group")
    : !string.IsNullOrWhiteSpace(environmentValue) ? new ResourceGroupChoice(environmentValue.Trim(), $"Resource group: {environmentValue.Trim()} from the {AspireDeploy.AzureResourceGroupVariable} environment variable")
    : !string.IsNullOrWhiteSpace(userSecretValue) ? new ResourceGroupChoice(userSecretValue.Trim(), $"Resource group: {userSecretValue.Trim()} from the AppHost user secret {AzureResourceGroupConfigurationKey}")
    : new ResourceGroupChoice(null, "");

  /// <summary>Refusal when no resource group is known, with the pwsh ways to name it.</summary>
  internal static string NoResourceGroupMessage(string verb, string appHostProject) =>
    $"No resource group for aca: pass it ({verb} --target aca --resource-group <name>), or set what `dev deploy --target aca` uses — "
    + $"${{env:{AspireDeploy.AzureResourceGroupVariable}}} = '<name>' for this pwsh session, or "
    + $"dotnet user-secrets set '{AzureResourceGroupConfigurationKey}' '<name>' --project '{appHostProject}'. "
    + "List them with: az group list --query \"[].name\" --output tsv";

  /// <summary><c>az containerapp show</c> of the ingress container app: its public FQDN.</summary>
  internal static string[] BuildIngressFqdnArguments(string resourceGroup, string subscriptionId) =>
  [
    "containerapp", "show", "--name", IngressResourceName, "--resource-group", resourceGroup, "--subscription", subscriptionId,
    "--query", "properties.configuration.ingress.fqdn", "--output", "tsv",
  ];

  /// <summary>The single trimmed value an <c>--output tsv</c> query printed; null when none (or the call failed).</summary>
  internal static string? ParseSingleValue(bool succeeded, string output)
  {
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return succeeded && lines.Length == 1 ? lines[0] : null;
  }

  internal static string[] BuildFlexibleServerListArguments(string resourceGroup, string subscriptionId) =>
    ["postgres", "flexible-server", "list", "--resource-group", resourceGroup, "--subscription", subscriptionId, "--query", "[].name", "--output", "tsv"];

  internal static string[] BuildKeyVaultListArguments(string resourceGroup, string subscriptionId) =>
  [
    "keyvault", "list", "--resource-group", resourceGroup, "--subscription", subscriptionId,
    "--query", $"[?tags.\"aspire-resource-name\"=='{AspireDeploy.PostgresKeyVaultTag}'].name", "--output", "tsv",
  ];

  internal static string[] BuildConnectionStringArguments(string vault, string subscriptionId) =>
    ["keyvault", "secret", "show", "--vault-name", vault, "--name", ConnectionStringSecret, "--subscription", subscriptionId, "--query", "value", "--output", "tsv"];

  internal static string[] BuildFirewallRuleCreateArguments(string resourceGroup, string server, string subscriptionId, string clientIp) =>
  [
    "postgres", "flexible-server", "firewall-rule", "create", "--resource-group", resourceGroup, "--name", server,
    "--subscription", subscriptionId, "--rule-name", FirewallRuleName, "--start-ip-address", clientIp, "--end-ip-address", clientIp,
  ];

  internal static string[] BuildFirewallRuleDeleteArguments(string resourceGroup, string server, string subscriptionId) =>
  [
    "postgres", "flexible-server", "firewall-rule", "delete", "--resource-group", resourceGroup, "--name", server,
    "--subscription", subscriptionId, "--rule-name", FirewallRuleName, "--yes",
  ];

  internal static string[] BuildBundleArguments(string connectionString) => ["--connection", connectionString];

  /// <summary>Arguments with every <c>--connection</c> value replaced, for printing.</summary>
  internal static string[] RedactConnection(IReadOnlyList<string> arguments) =>
    [.. arguments.Select((argument, index) => index > 0 && arguments[index - 1] == "--connection" ? "<connection string from Key Vault>" : argument)];

  internal static string NoFlexibleServerMessage(string resourceGroup, int count) =>
    count == 0
      ? $"No PostgreSQL Flexible Server in resource group {resourceGroup}. Deploy first (dev deploy --target aca), or name the right group with --resource-group."
      : $"{count} PostgreSQL Flexible Servers in resource group {resourceGroup}; the aca deployment provisions one. Check the group: "
        + $"az postgres flexible-server list --resource-group {resourceGroup} --output table";

  /// <summary>Refusal when the Key Vault secret cannot be read: grant yourself the role first (pwsh), the verb never does.</summary>
  internal static string ConnectionStringUnreadableMessage(string resourceGroup, string? vault) =>
    (vault is null
      ? $"No Key Vault tagged aspire-resource-name={AspireDeploy.PostgresKeyVaultTag} in resource group {resourceGroup}. "
      : $"Cannot read the Key Vault secret {ConnectionStringSecret} in {vault}. ")
    + "The vault uses RBAC and the deployment grants you no role; grant yourself Key Vault Secrets User once (pwsh), then re-run:\n"
    + $"  $vault = az keyvault list --resource-group {resourceGroup} --query \"[?tags.`\"aspire-resource-name`\"=='{AspireDeploy.PostgresKeyVaultTag}'].name\" --output tsv\n"
    + "  az role assignment create --assignee (az ad signed-in-user show --query id --output tsv) --role 'Key Vault Secrets User' "
    + "--scope (az keyvault show --name $vault --query id --output tsv)";

  /// <summary>The operator's IP: <c>--client-ip</c> when it parses, else the probe's answer; null when neither is an IP.</summary>
  internal static string? ChooseClientIp(string? option, string? probed)
  {
    string? candidate = string.IsNullOrWhiteSpace(option) ? probed?.Trim() : option.Trim();
    return candidate is not null && IPAddress.TryParse(candidate, out IPAddress? address)
      && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
      ? address.ToString()
      : null;
  }

  internal static string NoClientIpMessage(string? option) =>
    string.IsNullOrWhiteSpace(option)
      ? $"Could not determine your public IPv4 address ({ClientIpProbe} did not answer with one). Pass it: dev deploy migrate --target aca --client-ip <ipv4>"
      : $"--client-ip '{option}' is not an IPv4 address (Flexible Server firewall rules take IPv4).";

  /// <summary>
  /// Creates the <c>operator-migrate</c> firewall rule, runs the bundle, and ALWAYS deletes the rule (finally) — on a failed
  /// create, a failed bundle and an exception alike. The exit code is the first failure's (create, then bundle), else the
  /// delete's; <paramref name="run"/> executes one step and returns its exit code. A failed delete is passed to
  /// <paramref name="reportCleanupFailure"/> inside the finally, so it is reported even when an exception propagates.
  /// </summary>
  internal static async Task<AcaMigrationResult> RunAcaBundleAsync(
    ProcessStep createRule, ProcessStep bundle, ProcessStep deleteRule,
    Func<ProcessStep, CancellationToken, Task<int>> run, Action<string> reportCleanupFailure, CancellationToken cancellationToken)
  {
    int exitCode = 0;
    int cleanupExitCode = 0;
    string? cleanupFailure = null;
    try
    {
      exitCode = await run(createRule, cancellationToken).ConfigureAwait(false);
      if (exitCode == 0)
      {
        exitCode = await run(bundle, cancellationToken).ConfigureAwait(false);
      }
    }
    finally
    {
      try
      {
        // Not the caller's token: Ctrl+C during the bundle must still remove the rule.
        cleanupExitCode = await run(deleteRule, CancellationToken.None).ConfigureAwait(false);
      }
      catch (Exception exception) when (exception is not OutOfMemoryException)
      {
        cleanupExitCode = -1;
        cleanupFailure = exception.Message;
      }

      if (cleanupExitCode != 0)
      {
        cleanupFailure ??= $"exit {cleanupExitCode}";
        // Reported here, not from the result: an exception from create/bundle skips the return.
        reportCleanupFailure(cleanupFailure);
      }
    }

    return new AcaMigrationResult(exitCode != 0 ? exitCode : cleanupExitCode == 0 ? 0 : 1, cleanupExitCode, cleanupFailure);
  }

  /// <summary>Printed when the firewall rule could not be deleted: the exact command (pwsh-safe) to remove it by hand.</summary>
  internal static string FirewallCleanupFailedMessage(string resourceGroup, string server, string subscriptionId, string reason) =>
    $"The temporary firewall rule {FirewallRuleName} could NOT be deleted ({reason}); it still admits your IP. Delete it now: "
    + $"az {string.Join(' ', BuildFirewallRuleDeleteArguments(resourceGroup, server, subscriptionId))}";

  // ── confirmation and summary ───────────────────────────────────────────────

  internal const string MigrateConfirmationRefusal =
    "Not migrating: no confirmation. Re-run with --yes to migrate non-interactively, or from a terminal to answer the prompt.";

  internal const string MigrateDeclined = "Not migrating: declined at the prompt. Nothing was run.";

  /// <summary>The one summary line: what ran against which target, and how it ended.</summary>
  internal static string BuildMigrateSummary(DeployTarget target, string ran, string against, int exitCode) =>
    exitCode == 0
      ? $"dev deploy migrate: applied {ran} to {target.Name} ({against})."
      : $"dev deploy migrate: {ran} against {target.Name} ({against}) failed (exit {exitCode}).";

  // ── browser ────────────────────────────────────────────────────────────────

  /// <summary>
  /// Browser openers to try in order: Windows <c>explorer.exe</c>; macOS <c>open</c>; WSL <c>wslview</c> then
  /// <c>explorer.exe</c> (the Windows browser), then <c>xdg-open</c>; other Linux <c>xdg-open</c>. The first on PATH wins;
  /// none means print the URL.
  /// </summary>
  internal static BrowserLauncher[] BrowserLaunchers(bool isWindows, bool isMacOs, bool isWsl) =>
    isWindows ? [new BrowserLauncher("explorer.exe", [])]
    : isMacOs ? [new BrowserLauncher("open", [])]
    : isWsl ? [new BrowserLauncher("wslview", []), new BrowserLauncher("explorer.exe", []), new BrowserLauncher("xdg-open", [])]
    : [new BrowserLauncher("xdg-open", [])];

  /// <summary>WSL when WSL_DISTRO_NAME is set or the kernel version names Microsoft.</summary>
  internal static bool IsWsl(string? distroName, string? kernelVersion) =>
    !string.IsNullOrWhiteSpace(distroName)
    || (kernelVersion?.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ?? false);

  internal static string NoBrowserMessage(string url) => $"No browser opener found (or --no-browser); open {url}";

  internal static string ForwardNotReadyMessage(string url, int seconds) =>
    $"The port-forward is not listening after {seconds}s; the browser was not opened. Open {url} once it is (Ctrl+C stops the forward).";
}
