#region Purpose
// `dev deploy migrate [--target compose|kubernetes|aca] [--yes]`: apply the published idempotent EF
// migrations to the database of a deployment `dev deploy` made. Operator-run, never CI.
#endregion

#region Design
// Migrations are explicit in every deployed environment (AppHost Design region); this verb wraps the
// commands the tw-deploy skill lists, per target, and exits with the underlying tool's exit code
// (task 287). Like `dev deploy`, no workflow step or `dev workflow` mode may call it (dev-cli-tests
// NeverAutomated_Given_).
//   1. Preflight (PreflightScope.Migrate): kubernetes — the current kubectl context answers, kind's
//      cluster exists, and the deploy parameters resolve (k8s-namespace names the namespace); aca —
//      `az login` and the subscription with its source. No aspire/helm/registry checks: none of them runs.
//   2. The published artifact (artifacts/aspire-output/<target>/efmigrations/web-migrations.sql, or the
//      web-migrations bundle for aca) must exist. A missing one is REFUSED with the exact
//      `dev publish <target>` command; the verb never publishes (DeployOperate Design region says why).
//   3. Target specifics, all read-only so far:
//      compose — `<runtime> compose ls --format json` (runtime = ASPIRE_CONTAINER_RUNTIME, else docker;
//      never a hard-coded docker) picks the running project (--project-name, else the one from this
//      checkout, else the only one);
//      kubernetes — `kubectl --context <ctx> exec -i -n <k8s-namespace> statefulset/postgres-statefulset`;
//      aca — resource group (--resource-group, Azure__ResourceGroup, AppHost user secret
//      Azure:ResourceGroup), the one Flexible Server in it, the connection string from the postgres-kv
//      Key Vault secret (never printed; a missing role is refused with the pwsh grant), and the
//      operator's IPv4 (--client-ip, else api.ipify.org).
//   4. The plan (the command in pwsh form, the script piped with Get-Content -Raw) is printed and
//      confirmed unless --yes; with stdin redirected and no --yes it refuses.
//   5. compose / kubernetes pipe the script into psql inside the postgres container (PGPASSWORD from the
//      container's environment) — stdin is the script, so plain passthrough, not a TTY. aca runs
//      DeployOperate.RunAcaBundleAsync: create the operator-migrate firewall rule, run the bundle with
//      --connection on a real TTY, and delete the rule in finally; a failed delete is reported from the finally with the command to
//      run by hand, so it is printed even when Ctrl+C propagates.
//   6. One summary line: what ran against which target; the exit code is the tool's.
// The script and the bundle are idempotent (only pending migrations apply), so re-running is safe.
#endregion

namespace DevCli.Commands;

[NuruRoute("migrate", Description = "Apply the published idempotent migrations to a deployment's database (asks unless --yes; operator-run, never CI)")]
[NuruRouteExample("deploy migrate", Description = "psql the published script into the running compose stack's postgres")]
[NuruRouteExample("deploy migrate --target kubernetes", Description = "kubectl exec psql in statefulset/postgres-statefulset of the k8s-namespace parameter")]
[NuruRouteExample("deploy migrate --target aca --resource-group my-rg", Description = "Run the bundle through a temporary operator-migrate firewall rule (always deleted)")]
[NuruRouteExample("deploy migrate --target kubernetes --yes", Description = "Migrate without prompting")]
internal sealed class DeployMigrateCommand : DeployGroup, ICommand<Unit>
{
  [Option("target", "t", Description = "Deploy target: compose | kubernetes | aca (default: compose, the AppHost's Publish:Target default)")]
  public string? Target { get; set; }

  [Option("yes", "y", Description = "Confirm without prompting")]
  public bool Yes { get; set; }

  [Option("project-name", Description = "compose: the Compose project to migrate (default: the running project from this checkout, else the only one)")]
  public string? ProjectName { get; set; }

  [Option("resource-group", "g", Description = "aca: resource group (default: Azure__ResourceGroup, else the AppHost user secret Azure:ResourceGroup)")]
  public string? ResourceGroup { get; set; }

  [Option("client-ip", Description = "aca: your public IPv4 for the temporary firewall rule (default: what https://api.ipify.org reports)")]
  public string? ClientIp { get; set; }

  internal sealed class Handler : ICommandHandler<DeployMigrateCommand, Unit>
  {
    private const string Verb = "dev deploy migrate";
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(DeployMigrateCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      DeployPreflight? preflight = await AspireDeployPreflight.RunAsync(Terminal, Verb, command.Target, PreflightScope.Migrate, ct);
      if (preflight is null) return Unit.Value;

      string artifact = Path.Combine(preflight.RepoRoot, DeployOperate.PublishedOutputDirectory(preflight.Target), DeployOperate.MigrationArtifact(preflight.Target));
      if (OperatingSystem.IsWindows() && preflight.Target == AspireDeploy.ContainerApps && !File.Exists(artifact))
      {
        artifact += ".exe";
      }

      if (!File.Exists(artifact))
      {
        Fail(DeployOperate.MissingPublishedOutputMessage(preflight.Target, artifact));
        return Unit.Value;
      }

      if (preflight.Target == AspireDeploy.ContainerApps)
      {
        await MigrateContainerAppsAsync(command, preflight, artifact, ct);
        return Unit.Value;
      }

      string executable;
      string[] arguments;
      string against;
      if (preflight.Target == AspireDeploy.Kubernetes)
      {
        string kubernetesNamespace = preflight.Parameters.First(parameter => parameter.Name == AspireDeploy.KubernetesNamespaceParameter).Value;
        executable = "kubectl";
        arguments = DeployOperate.BuildKubectlMigrateArguments(preflight.KubectlContext!, kubernetesNamespace);
        against = $"context {preflight.KubectlContext}, namespace {kubernetesNamespace}";
      }
      else
      {
        string runtime = AspireDeploy.ContainerRuntime(Environment.GetEnvironmentVariable(AspireDeploy.ContainerRuntimeVariable));
        string[] listArguments = DeployOperate.BuildComposeListArguments();
        CommandOutput? list = await AspireDeployPreflight.ProbeAsync(runtime, listArguments, ct);
        if (list is not { Success: true })
        {
          Fail($"`{runtime} compose ls` failed ({(list is null ? $"{runtime} is not on PATH" : AspireDeploy.FirstLine(list.Stderr, $"exit {list.ExitCode}"))}). "
            + $"{AspireDeploy.ContainerRuntimeVariable} selects the runtime; pass the project with --project-name if your runtime has no `compose ls`.");
          return Unit.Value;
        }

        ComposeProjectChoice choice = DeployOperate.ChooseComposeProject(
          DeployOperate.ParseComposeProjects(list.Stdout), command.ProjectName, preflight.RepoRoot, runtime);
        if (choice.Project is null)
        {
          Fail(choice.Refusal!);
          return Unit.Value;
        }

        executable = runtime;
        arguments = DeployOperate.BuildComposeMigrateArguments(choice.Project);
        against = $"Compose project {choice.Project.Name}";
      }

      Terminal.WriteLine($"{Verb} → {preflight.Target.Name}");
      Terminal.WriteLine($"  {preflight.Detail}");
      Terminal.WriteLine($"  Script:  {artifact}");
      Terminal.WriteLine($"  Runs:    {DeployOperate.BuildPipedCommandDisplay(artifact, executable, arguments)}");
      if (!Confirmed(command.Yes)) return Unit.Value;

      CommandOutput result = await Shell.Builder(executable)
        .WithArguments(arguments)
        .WithStandardInput(await File.ReadAllTextAsync(artifact, ct))
        .WithWorkingDirectory(preflight.RepoRoot)
        .WithNoValidation()
        .PassthroughAsync(ct);

      Summarize(preflight.Target, $"{Path.GetFileName(artifact)} with psql", against, result.ExitCode);
      return Unit.Value;
    }

    private async Task MigrateContainerAppsAsync(DeployMigrateCommand command, DeployPreflight preflight, string bundle, CancellationToken ct)
    {
      string subscription = preflight.AzureSubscriptionId!;
      string? environmentGroup = Environment.GetEnvironmentVariable(AspireDeploy.AzureResourceGroupVariable);
      ResourceGroupChoice resourceGroup = DeployOperate.ChooseResourceGroup(
        command.ResourceGroup,
        environmentGroup,
        string.IsNullOrWhiteSpace(command.ResourceGroup) && string.IsNullOrWhiteSpace(environmentGroup)
          ? await AspireDeployPreflight.ReadUserSecretAsync(preflight.AppHostProject, DeployOperate.AzureResourceGroupConfigurationKey, ct)
          : null);
      if (resourceGroup.Name is null)
      {
        Fail(DeployOperate.NoResourceGroupMessage(Verb, preflight.AppHostProject));
        return;
      }

      string group = resourceGroup.Name;
      CommandOutput? servers = await AspireDeployPreflight.ProbeAsync("az", DeployOperate.BuildFlexibleServerListArguments(group, subscription), ct);
      string[] serverNames = servers?.Success == true
        ? servers.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : [];
      if (servers is not { Success: true })
      {
        Fail($"`az postgres flexible-server list --resource-group {group}` failed ({AspireDeploy.FirstLine(servers?.Stderr ?? "", "az is not on PATH or timed out")}).");
        return;
      }

      if (serverNames.Length != 1)
      {
        Fail(DeployOperate.NoFlexibleServerMessage(group, serverNames.Length));
        return;
      }

      string server = serverNames[0];
      CommandOutput? vaults = await AspireDeployPreflight.ProbeAsync("az", DeployOperate.BuildKeyVaultListArguments(group, subscription), ct);
      string? vault = DeployOperate.ParseSingleValue(vaults?.Success == true, vaults?.Stdout ?? "");
      CommandOutput? secret = vault is null ? null : await AspireDeployPreflight.ProbeAsync("az", DeployOperate.BuildConnectionStringArguments(vault, subscription), ct);
      string? connectionString = DeployOperate.ParseSingleValue(secret?.Success == true, secret?.Stdout ?? "");
      if (connectionString is null)
      {
        Fail(DeployOperate.ConnectionStringUnreadableMessage(group, vault));
        return;
      }

      string? clientIp = DeployOperate.ChooseClientIp(command.ClientIp, string.IsNullOrWhiteSpace(command.ClientIp) ? await ProbeClientIpAsync(ct) : null);
      if (clientIp is null)
      {
        Fail(DeployOperate.NoClientIpMessage(command.ClientIp));
        return;
      }

      ProcessStep create = new("az", DeployOperate.BuildFirewallRuleCreateArguments(group, server, subscription, clientIp));
      ProcessStep run = new(bundle, DeployOperate.BuildBundleArguments(connectionString), Terminal: true);
      ProcessStep delete = new("az", DeployOperate.BuildFirewallRuleDeleteArguments(group, server, subscription));

      Terminal.WriteLine($"{Verb} → aca");
      Terminal.WriteLine($"  {preflight.Detail}");
      Terminal.WriteLine($"  {resourceGroup.Detail}");
      Terminal.WriteLine($"  Server:  {server} (connection string from Key Vault {vault}, not shown)");
      Terminal.WriteLine($"  Bundle:  {bundle}");
      Terminal.WriteLine($"  Firewall rule {DeployOperate.FirewallRuleName} for {clientIp} is created for the run and always deleted afterwards.");
      if (!Confirmed(command.Yes)) return;

      AcaMigrationResult result = await DeployOperate.RunAcaBundleAsync(
        create, run, delete, RunStepAsync,
        reason => Terminal.WriteErrorLine(DeployOperate.FirewallCleanupFailedMessage(group, server, subscription, reason).Red()),
        ct);

      Summarize(AspireDeploy.ContainerApps, "the web-migrations bundle", $"server {server} in {group}", result.ExitCode);
    }

    private async Task<int> RunStepAsync(ProcessStep step, CancellationToken ct)
    {
      Terminal.WriteLine($"\n{step.Executable} {string.Join(' ', DeployOperate.RedactConnection(step.Arguments).Select(DeployOperate.PwshQuote))}");
      ShellBuilder builder = Shell.Builder(step.Executable)
        .WithArguments([.. step.Arguments])
        .WithNoValidation();
      CommandOutput output = step.Terminal ? await builder.TtyPassthroughAsync(ct) : await builder.PassthroughAsync(ct);
      return output.ExitCode;
    }

    private static async Task<string?> ProbeClientIpAsync(CancellationToken ct)
    {
      using System.Net.Http.HttpClient client = new() { Timeout = TimeSpan.FromSeconds(10) };
      try
      {
        return await client.GetStringAsync(DeployOperate.ClientIpProbe, ct);
      }
      catch (Exception exception) when (exception is System.Net.Http.HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
      {
        return null;
      }
    }

    private bool Confirmed(bool yes)
    {
      if (yes) return true;
      if (Terminal.IsInputRedirected)
      {
        Fail(DeployOperate.MigrateConfirmationRefusal);
        return false;
      }

      Terminal.Write("Migrate? [y/N] ");
      string? answer = Terminal.ReadLine()?.Trim();
      if (string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      Fail(DeployOperate.MigrateDeclined);
      return false;
    }

    private void Summarize(DeployTarget target, string ran, string against, int exitCode)
    {
      string line = DeployOperate.BuildMigrateSummary(target, ran, against, exitCode);
      if (exitCode == 0)
      {
        Terminal.WriteLine($"\n{line}".Green());
        return;
      }

      Terminal.WriteErrorLine($"\n{line}".Red());
      Environment.ExitCode = exitCode;
    }

    private void Fail(string message)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = 1;
    }
  }
}
