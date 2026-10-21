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
// derived from them (statefulset/postgres-statefulset); dev-cli-tests reads constants.cs so the copies
// cannot drift.
//
// Ingress port (task 311): no default lives here. The port is the AppHost's ingress-port parameter,
// committed in the Parameters section of its appsettings.json (the template's ingressPort port
// generator picks one per generated app) and resolved like dev deploy's parameters —
// env, user secret, appsettings.Production.json, appsettings.json. One number per app: the Compose host
// port, the `dev open --target kubernetes` forward port and the compose fallback. --port overrides it;
// with neither, `dev open` refuses (NoIngressPortMessage) rather than guess a shared port such as 8080.
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
// Report (task 339): the idempotent script's transcript looks the same whether ten migrations applied or
// none, so the verb reports from EF's documented state instead — `__EFMigrationsHistory` read with psql
// before and after the run through the same exec path (a missing table, SQLSTATE 42P01, is a fresh
// database: none applied). The script itself runs quietly (-q, client_min_messages=warning) so only errors
// and WARNINGs print; --verbose keeps psql's full transcript. psql's DO lines are never parsed. ACA has no
// psql on the operator's machine; the bundle's own `Applying migration '…'` lines (EF's log output) give
// its summary.
//
// Stale script (task 341): "Up to date" is only true relative to the published script, which may come from an
// older `dev publish`. Before the plan, the verb compares the migration ids in the source (file names in the
// AppHost's sibling apps' platform/postgres/migrations folders — FindSourceMigrationFolders, not one hard-coded
// app; none means no postgres, so the check is skipped) with the ids in the script's __EFMigrationsHistory
// inserts (EF's stable idempotent-script output). Source ids the script lacks print StaleScriptWarning before
// the prompt; --yes then REFUSES unless --allow-stale, so an unattended run never silently applies an old
// script; and the report says "relative to the published script". aca: the bundle is a compiled binary whose
// migration list is not readable without EF internals, but the same publish writes web-migrations.sql beside it
// (PublishAsMigrationScript runs for every target), so the check reads that script; with no script beside the
// bundle it says the check is unavailable and skips it.
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

/// <summary>One process the aca migration runs: executable, arguments, and whether its output is captured for the summary.</summary>
internal sealed record ProcessStep(string Executable, IReadOnlyList<string> Arguments, bool Capture = false);

/// <summary>Outcome of the aca bundle run: the exit code to report, and the firewall-rule delete's own exit code.</summary>
internal sealed record AcaMigrationResult(int ExitCode, int CleanupExitCode, string? CleanupFailure);

/// <summary>A browser opener candidate: executable plus the arguments before the URL.</summary>
internal sealed record BrowserLauncher(string Executable, IReadOnlyList<string> Arguments);

/// <summary>Builds and decides the <c>dev open</c> / <c>dev deploy migrate</c> invocations per deploy target.</summary>
internal static partial class DeployOperate
{
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

  internal const string MigrationScript = "efmigrations/web-migrations.sql";
  internal const string MigrationBundle = "efmigrations/web-migrations";
  internal const string FirewallRuleName = "operator-migrate";
  internal const string ConnectionStringSecret = "connectionstrings--postgres-db";
  internal const string AzureResourceGroupConfigurationKey = "Azure:ResourceGroup";
  internal static readonly Uri ClientIpProbe = new("https://api.ipify.org");

  /// <summary>psql inside the postgres container; the password comes from the container's environment.</summary>
  private const string Psql = "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d " + PostgresDatabaseName;

  /// <summary>
  /// The script run with psql's full transcript (<c>--verbose</c>): every statement tag and NOTICE, as psql prints them.
  /// </summary>
  internal const string PsqlTranscriptCommand = Psql + " -v ON_ERROR_STOP=1";

  /// <summary>
  /// The script run quietly (the default): <c>-q</c> drops the statement tags and <c>client_min_messages=warning</c> drops the
  /// NOTICEs (the idempotent script's "__EFMigrationsHistory already exists, skipping"); errors and WARNINGs still print.
  /// </summary>
  internal const string PsqlQuietCommand =
    "PGPASSWORD=\"$POSTGRES_PASSWORD\" PGOPTIONS='-c client_min_messages=warning' psql -U postgres -d " + PostgresDatabaseName
    + " -q -v ON_ERROR_STOP=1";

  internal const string MigrationHistoryTable = "__EFMigrationsHistory";

  /// <summary>
  /// Reads EF's applied migration ids, one per line, oldest first. <c>VERBOSITY=sqlstate</c> makes a missing table (fresh
  /// database) print <see cref="UndefinedTableSqlState"/> instead of localized text.
  /// </summary>
  internal const string PsqlHistoryCommand =
    Psql + " -At -v VERBOSITY=sqlstate -c 'SELECT \"MigrationId\" FROM \"" + MigrationHistoryTable + "\" ORDER BY \"MigrationId\"'";

  /// <summary>PostgreSQL's SQLSTATE for undefined_table.</summary>
  internal const string UndefinedTableSqlState = "42P01";

  /// <summary>The psql command that runs the script: quiet unless <paramref name="verbose"/>.</summary>
  internal static string PsqlScriptCommand(bool verbose) => verbose ? PsqlTranscriptCommand : PsqlQuietCommand;

  /// <summary>Where <c>dev publish &lt;target&gt;</c> writes the target's output, relative to the repository root.</summary>
  internal static string PublishedOutputDirectory(DeployTarget target) => $"artifacts/aspire-output/{target.Name}";

  /// <summary>The published file <c>dev deploy migrate</c> runs for <paramref name="target"/>: the bundle for aca, else the SQL script.</summary>
  internal static string MigrationArtifact(DeployTarget target) =>
    AspireDeploy.UsesMigrationBundle(target) ? MigrationBundle : MigrationScript;

  /// <summary>Refusal when the published migration artifact is missing: the exact command that writes it.</summary>
  internal static string MissingPublishedOutputMessage(DeployTarget target, string path) =>
    $"No published migrations for {target.Name}: {path} does not exist. Publish them first (the verb never publishes for you), then re-run: "
    + $"dev publish {target.Name}";

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

    string source = $"{IngressPortParameter} from the {parameter.Source}";
    (int? parameterPort, string? parameterError) = ParsePort(parameter.Value, source);
    return (parameterPort, parameterError ?? source);
  }

  /// <summary>Refusal when neither <c>--port</c> nor the AppHost's <c>ingress-port</c> parameter gives a port.</summary>
  internal static string NoIngressPortMessage(string appHostProject) =>
    $"No ingress port: the AppHost parameter {IngressPortParameter} is not set. Commit it in the {AspireDeploy.ParametersSection} section of "
    + $"{Path.Combine(Path.GetDirectoryName(appHostProject) ?? "", AspireDeploy.AppSettingsFileName)}, set it per machine with "
    + $"`dotnet user-secrets set '{AspireDeploy.ParameterConfigurationKey(IngressPortParameter)}' '<port>' --project '{appHostProject}'`, or pass --port <n>.";

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
  /// <c>kubectl exec</c> of <paramref name="psqlCommand"/> (<see cref="PsqlScriptCommand"/> or <see cref="PsqlHistoryCommand"/>)
  /// in the postgres StatefulSet of <paramref name="kubernetesNamespace"/>, stdin attached for the script.
  /// </summary>
  internal static string[] BuildKubectlMigrateArguments(string context, string kubernetesNamespace, string psqlCommand) =>
    ["--context", context, "exec", "-i", "--namespace", kubernetesNamespace, PostgresStatefulSet, "--", "sh", "-c", psqlCommand];

  // ── compose ────────────────────────────────────────────────────────────────

  /// <summary>The value of <paramref name="name"/> in a <c>.env</c> file; null when absent or blank (the published .env leaves parameters blank).</summary>
  internal static string? ParseEnvValue(string envText, string name)
  {
    foreach (string raw in envText.Split('\n'))
    {
      string line = raw.Trim();
      if (line.StartsWith('#', StringComparison.Ordinal))
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
  /// <c>ingress-port</c> parameter. Returns the port and where it came from; (null, null) when none is set.
  /// </summary>
  internal static (string? Port, string? Source) ChooseComposePort(string? option, string? envValue, ResolvedDeployParameter? parameter) =>
    !string.IsNullOrWhiteSpace(option) ? (option.Trim(), "--port")
    : envValue is not null ? (envValue, $"{IngressPortVariable} in the published .env")
    : parameter is not null ? (parameter.Value, $"{IngressPortParameter} from the {parameter.Source}")
    : (null, null);

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

  /// <summary>The migrate command as an operator would type it in pwsh: the script piped on stdin (<c>Get-Content -Raw</c>).</summary>
  internal static string BuildPipedCommandDisplay(string script, string executable, IReadOnlyList<string> arguments) =>
    $"Get-Content -Raw {PwshQuote(script)} | {executable} {string.Join(' ', arguments.Select(PwshQuote))}";

  /// <summary>pwsh single-quoted literal (embedded ' doubled) when the argument needs quoting; as-is otherwise.</summary>
  internal static string PwshQuote(string argument) =>
    argument.Length > 0 && argument.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '/' or ':' or '=')
      ? argument
      : $"'{argument.Replace("'", "''", StringComparison.Ordinal)}'";

  /// <summary>
  /// <c>&lt;runtime&gt; compose … exec -T postgres</c> of <paramref name="psqlCommand"/> for <paramref name="project"/>, stdin
  /// attached for the script.
  /// </summary>
  internal static string[] BuildComposeMigrateArguments(ComposeProject project, string psqlCommand) =>
  [
    "compose", "--project-name", project.Name,
    .. project.ConfigFiles.SelectMany(file => (string[])["--file", file]),
    "exec", "-T", PostgresResourceName, "sh", "-c", psqlCommand,
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

  /// <summary>The bundle's arguments; <paramref name="verbose"/> adds the bundle's own <c>--verbose</c>.</summary>
  internal static string[] BuildBundleArguments(string connectionString, bool verbose = false) =>
    verbose ? ["--connection", connectionString, "--verbose"] : ["--connection", connectionString];

  /// <summary>Arguments with every <c>--connection</c> value replaced, for printing.</summary>
  internal const string KeyVaultConnectionPlaceholder = "<connection string from Key Vault>";

  /// <summary>Printed in place of the direct Neon connection string.</summary>
  internal const string NeonDirectConnectionPlaceholder = "<direct Neon connection string>";

  /// <summary>Arguments with every <c>--connection</c> value replaced, for printing.</summary>
  internal static string[] RedactConnection(IReadOnlyList<string> arguments, string placeholder = KeyVaultConnectionPlaceholder) =>
    [.. arguments.Select((argument, index) => index > 0 && arguments[index - 1] == "--connection" ? placeholder : argument)];

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

  // ── stale published script ─────────────────────────────────────────────────

  /// <summary>Under an app folder (a sibling of the AppHost's tree): where EF writes that app's Postgres migrations.</summary>
  internal static readonly string SourceMigrationsSubpath = Path.Combine("platform", "postgres", "migrations");

  internal const string RelativeToPublishedScript = "relative to the published script";

  /// <summary>
  /// The <c>platform/postgres/migrations</c> folders of the AppHost's sibling apps: the nearest ancestor of the AppHost
  /// (up to <paramref name="repoRoot"/>) with a child app holding one. Empty when none exists (an app without postgres).
  /// </summary>
  internal static string[] FindSourceMigrationFolders(string appHostProject, string repoRoot)
  {
    string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repoRoot));
    for (DirectoryInfo? directory = new FileInfo(Path.GetFullPath(appHostProject)).Directory;
      directory?.FullName.StartsWith(root, StringComparison.Ordinal) == true;
      directory = directory.Parent)
    {
      string[] folders =
      [
        .. directory.EnumerateDirectories()
          .Select(app => Path.Combine(app.FullName, SourceMigrationsSubpath))
          .Where(Directory.Exists)
          .Order(StringComparer.Ordinal),
      ];
      if (folders.Length > 0) return folders;
    }

    return [];
  }

  /// <summary>
  /// Migration ids from a migrations folder's file names: <c>&lt;timestamp&gt;_&lt;Name&gt;.cs</c>, without the
  /// <c>.Designer.cs</c> files and the model snapshot; ordered.
  /// </summary>
  internal static string[] ParseSourceMigrationIds(IEnumerable<string> fileNames) =>
  [
    .. fileNames
      .Select(name => SourceMigrationFilePattern().Match(Path.GetFileName(name)))
      .Where(match => match.Success)
      .Select(match => match.Groups[1].Value)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal),
  ];

  /// <summary>The ids the published script records: each migration's <c>__EFMigrationsHistory</c> insert's <c>'&lt;MigrationId&gt;'</c>.</summary>
  internal static string[] ParseScriptMigrationIds(string script) =>
  [
    .. ScriptHistoryInsertPattern().Matches(script)
      .Select(match => match.Groups[1].Value)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal),
  ];

  /// <summary>Source ids the script lacks, ordered: non-empty means the published script is older than the source.</summary>
  internal static string[] MigrationsMissingFromScript(IEnumerable<string> sourceIds, IEnumerable<string> scriptIds)
  {
    HashSet<string> published = new(scriptIds, StringComparer.Ordinal);
    return [.. sourceIds.Where(id => !published.Contains(id)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
  }

  /// <summary>The warning printed before the confirmation when the published script is stale.</summary>
  internal static string StaleScriptWarning(DeployTarget target, IReadOnlyList<string> missing) =>
    $"⚠ Published script is stale: source has {missing.Count} migration(s) the script does not\n"
    + $"  ({string.Join(", ", missing)}). Run `dev publish {target.Name}` first,\n"
    + "  then redeploy so the app and schema match.";

  /// <summary>Unattended (<c>--yes</c>) runs never apply a stale script unless <c>--allow-stale</c> says so.</summary>
  internal static bool RefuseStaleUnattended(bool stale, bool yes, bool allowStale) => stale && yes && !allowStale;

  internal static string StaleUnattendedRefusal(DeployTarget target) =>
    $"Not migrating: the published script is stale and --yes was passed. Run `dev publish {target.Name}` and redeploy, "
    + "or pass --allow-stale to apply the old script anyway.";

  /// <summary>Printed for aca when no script sits beside the bundle: the bundle's own migrations are not listed.</summary>
  internal const string BundleStaleCheckUnavailable =
    "Stale check not available for the bundle: no web-migrations.sql beside it to read its migration ids from; skipped.";

  /// <summary>
  /// A successful report qualified "relative to the published script" (after a stale warning): the "Up to date" and
  /// "Applied" lines say what they are measured against.
  /// </summary>
  internal static string[] QualifyRelativeToPublishedScript(IReadOnlyList<string> lines) =>
  [
    .. lines.Select(line =>
      line.StartsWith("Up to date:", StringComparison.Ordinal) ? $"Up to date {RelativeToPublishedScript}:{line["Up to date:".Length..]}"
      : line.StartsWith("Applied ", StringComparison.Ordinal) && line.EndsWith(" migration(s):", StringComparison.Ordinal)
        ? $"{line[..^1]} {RelativeToPublishedScript}:"
      : line),
  ];

  [System.Text.RegularExpressions.GeneratedRegex(@"^(\d+_[A-Za-z0-9_]+)\.cs$")]
  private static partial System.Text.RegularExpressions.Regex SourceMigrationFilePattern();

  [System.Text.RegularExpressions.GeneratedRegex(@"INSERT\s+INTO\s+""__EFMigrationsHistory""\s*\(\s*""MigrationId""\s*,\s*""ProductVersion""\s*\)\s*VALUES\s*\(\s*'([^']+)'")]
  private static partial System.Text.RegularExpressions.Regex ScriptHistoryInsertPattern();

  // ── confirmation and summary ───────────────────────────────────────────────

  internal const string MigrateConfirmationRefusal =
    "Not migrating: no confirmation. Re-run with --yes to migrate non-interactively, or from a terminal to answer the prompt.";

  internal const string MigrateDeclined = "Not migrating: declined at the prompt. Nothing was run.";

  /// <summary>
  /// The applied ids from <see cref="PsqlHistoryCommand"/>: its lines on success, empty when the table does not exist yet
  /// (<see cref="UndefinedTableSqlState"/>, a fresh database), null when the history could not be read.
  /// </summary>
  internal static string[]? ParseMigrationHistory(int exitCode, string stdout, string stderr) =>
    exitCode == 0 ? stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : stderr.Contains(UndefinedTableSqlState, StringComparison.Ordinal) ? []
    : null;

  /// <summary>The ids in <paramref name="after"/> that are not in <paramref name="before"/>, in <paramref name="after"/>'s order.</summary>
  internal static string[] NewlyAppliedMigrations(IReadOnlyList<string> before, IReadOnlyList<string> after)
  {
    HashSet<string> already = new(before, StringComparer.Ordinal);
    return [.. after.Where(id => !already.Contains(id))];
  }

  /// <summary>
  /// What the run changed, from <c>__EFMigrationsHistory</c> read before and after it (null = unreadable): the ids applied,
  /// the count already applied and the latest id; or "Up to date"; or, on failure, the last id applied before it failed.
  /// </summary>
  internal static string[] BuildMigrationReport(IReadOnlyList<string>? before, IReadOnlyList<string>? after, int exitCode)
  {
    if (exitCode != 0)
    {
      string[] applied = before is not null && after is not null ? NewlyAppliedMigrations(before, after) : [];
      string failed = after is null
        ? $"Failed while applying migrations ({MigrationHistoryTable} could not be read after the failure)"
        : after.Count == 0 ? "Failed before any migration was applied"
        : $"Failed while applying migrations after {after[^1]}";
      return applied.Length == 0 ? [failed] : [.. AppliedLines(applied), failed];
    }

    if (before is null || after is null)
    {
      return [$"Migrations ran, but {MigrationHistoryTable} could not be read {(before is null ? "before" : "after")} the run, so which ones applied is unknown"];
    }

    string[] newlyApplied = NewlyAppliedMigrations(before, after);
    string latest = after.Count == 0 ? "" : after[^1];
    if (newlyApplied.Length == 0)
    {
      return [after.Count == 0 ? "Up to date: no pending migrations (0 applied)" : $"Up to date: no pending migrations ({after.Count} applied, latest {latest})"];
    }

    return [.. AppliedLines(newlyApplied), $"Already applied: {after.Count - newlyApplied.Length} · Database now at {latest}"];

    static IEnumerable<string> AppliedLines(string[] ids) => [$"Applied {ids.Length} migration(s):", .. ids.Select(id => $"  {id}")];
  }

  /// <summary>
  /// The ACA summary from the EF bundle's own output: the ids of its <c>Applying migration '…'</c> lines, or "Up to date"
  /// when there are none; on failure, the last id it started.
  /// </summary>
  internal static string[] BuildBundleReport(string bundleOutput, int exitCode)
  {
    string[] applying =
    [
      .. bundleOutput.Split('\n')
        .Select(line => BundleApplyingPattern().Match(line))
        .Where(match => match.Success)
        .Select(match => match.Groups[1].Value),
    ];
    if (exitCode != 0)
    {
      // The last "Applying migration" is the one that failed (each runs in its own transaction); the ones before it applied.
      if (applying.Length == 0) return ["Failed before any migration was applied"];
      string[] appliedBeforeFailure = applying[..^1];
      return appliedBeforeFailure.Length == 0
        ? [$"Failed while applying {applying[^1]}"]
        : [$"Applied {appliedBeforeFailure.Length} migration(s):", .. appliedBeforeFailure.Select(id => $"  {id}"), $"Failed while applying {applying[^1]}"];
    }

    return applying.Length == 0
      ? ["Up to date: no pending migrations"]
      : [$"Applied {applying.Length} migration(s):", .. applying.Select(id => $"  {id}"), $"Database now at {applying[^1]}"];
  }

  [System.Text.RegularExpressions.GeneratedRegex(@"Applying migration '([^']+)'")]
  private static partial System.Text.RegularExpressions.Regex BundleApplyingPattern();

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
