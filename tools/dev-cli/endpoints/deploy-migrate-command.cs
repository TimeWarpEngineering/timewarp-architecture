#region Purpose
// `dev deploy migrate [--target compose|kubernetes|aca|aca-neon] [--yes] [--allow-stale]`: apply the published idempotent EF
// migrations to the database of a deployment `dev deploy` made. Operator-run, never CI.
#endregion

#region Design
// Migrations are explicit in every deployed environment (AppHost Design region); this verb wraps the
// commands the tw-deploy skill lists, per target, and exits with the underlying tool's exit code
// (task 287). Like `dev deploy`, no workflow step or `dev workflow` mode may call it (dev-cli-tests
// NeverAutomated_Given_).
//   1. Preflight (PreflightScope.Migrate): kubernetes — the current kubectl context answers, kind's
//      cluster exists, and the deploy parameters resolve (k8s-namespace names the namespace); aca —
//      `az login` and the subscription with its source; aca-neon — the direct Neon string only, no
//      `az login` and no pooled string. No aspire/helm/registry checks: none of them runs.
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
//      aca-neon — the published bundle with --connection set to the direct Neon string from preflight
//      secrets (never printed). No firewall rule, no Key Vault and no `az postgres`.
//   3a. Stale check (task 341, DeployOperate Design region): source migration ids vs the published script's;
//      missing ones print the warning before the prompt, --yes refuses on them unless --allow-stale, and the
//      report then says "relative to the published script". aca and aca-neon read the script beside the bundle, else skip.
//   4. The plan (the command in pwsh form, the script piped with Get-Content -Raw) is printed and
//      confirmed unless --yes; with stdin redirected and no --yes it refuses.
//   5. compose / kubernetes read __EFMigrationsHistory, pipe the script into psql inside the postgres
//      container (PGPASSWORD from the container's environment; quiet unless --verbose) — stdin is the
//      script, so plain passthrough, not a TTY — and read the history again. aca runs
//      DeployOperate.RunAcaBundleAsync: create the operator-migrate firewall rule, run the bundle with
//      --connection (output streamed and captured for the summary), and delete the rule in finally; a failed
//      delete is reported from the finally with the command to run by hand, so it is printed even when
//      Ctrl+C propagates.
//      aca-neon runs only the bundle. The printed command replaces the connection string with the Neon
//      placeholder, not the Key Vault one.
//   6. The report (DeployOperate.BuildMigrationReport / BuildBundleReport: applied ids, "Up to date", or
//      where it failed), then one summary line: what ran against which target; the exit code is the tool's.
// The script and the bundle are idempotent (only pending migrations apply), so re-running is safe.
#endregion

namespace DevCli.Commands;

[NuruRoute("migrate", Description = "Apply the published idempotent migrations to a deployment's database (asks unless --yes; operator-run, never CI)")]
[NuruRouteExample("deploy migrate", Description = "psql the published script into the running compose stack's postgres")]
[NuruRouteExample("deploy migrate --target kubernetes", Description = "kubectl exec psql in statefulset/postgres-statefulset of the k8s-namespace parameter")]
[NuruRouteExample("deploy migrate --target aca --resource-group my-rg", Description = "Run the bundle through a temporary operator-migrate firewall rule (always deleted)")]
[NuruRouteExample("deploy migrate --target aca-neon", Description = "Run the bundle with the direct Neon connection string (no firewall rule)")]
[NuruRouteExample("deploy migrate --target kubernetes --yes", Description = "Migrate without prompting")]
[NuruRouteExample("deploy migrate --target kubernetes --verbose", Description = "Show psql's full transcript instead of only errors and the summary")]
internal sealed class DeployMigrateCommand : DeployGroup, ICommand<Unit>
{
  [Option("target", "t", Description = "Deploy target: compose | kubernetes | aca | aca-neon (default: compose, the AppHost's Publish:Target default)")]
  public string? Target { get; set; }

  [Option("yes", "y", Description = "Confirm without prompting")]
  public bool Yes { get; set; }

  [Option("allow-stale", Description = "With --yes: apply the published script even when the source has migrations it lacks (default: refuse)")]
  public bool AllowStale { get; set; }

  [Option("project-name", Description = "compose: the Compose project to migrate (default: the running project from this checkout, else the only one)")]
  public string? ProjectName { get; set; }

  [Option("resource-group", "g", Description = "aca: resource group (default: Azure__ResourceGroup, else the AppHost user secret Azure:ResourceGroup)")]
  public string? ResourceGroup { get; set; }

  [Option("verbose", Description = "Show the full transcript: psql's statement tags and NOTICEs (compose/kubernetes), the bundle's --verbose (aca and aca-neon)")]
  public bool Verbose { get; set; }

  [Option("client-ip", Description = "aca: your public IPv4 for the temporary firewall rule (default: what https://api.ipify.org reports)")]
  public string? ClientIp { get; set; }

  internal sealed class Handler : ICommandHandler<DeployMigrateCommand, Unit>
  {
    private const string Verb = "dev deploy migrate";
    private readonly ITerminal Terminal;
    private string BundleOutput = "";
    private bool Stale;

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
      if (OperatingSystem.IsWindows() && AspireDeploy.IsAzureContainerApps(preflight.Target) && !File.Exists(artifact))
      {
        artifact += ".exe";
      }

      if (!File.Exists(artifact))
      {
        Fail(DeployOperate.MissingPublishedOutputMessage(preflight.Target, artifact));
        return Unit.Value;
      }

      string script = AspireDeploy.UsesMigrationBundle(preflight.Target)
        ? Path.Combine(preflight.RepoRoot, DeployOperate.PublishedOutputDirectory(preflight.Target), DeployOperate.MigrationScript)
        : artifact;
      if (!await CheckStaleAsync(command, preflight, script, ct)) return Unit.Value;

      if (preflight.Target == AspireDeploy.ContainerApps)
      {
        await MigrateContainerAppsAsync(command, preflight, artifact, ct);
        return Unit.Value;
      }

      if (preflight.Target == AspireDeploy.ContainerAppsNeon)
      {
        await MigrateNeonAsync(command, preflight, artifact, ct);
        return Unit.Value;
      }

      string executable;
      string[] arguments;
      string[] historyArguments;
      string against;
      if (preflight.Target == AspireDeploy.Kubernetes)
      {
        string kubernetesNamespace = preflight.Parameters.First(parameter => parameter.Name == AspireDeploy.KubernetesNamespaceParameter).Value;
        executable = "kubectl";
        arguments = DeployOperate.BuildKubectlMigrateArguments(preflight.KubectlContext!, kubernetesNamespace, DeployOperate.PsqlScriptCommand(command.Verbose));
        historyArguments = DeployOperate.BuildKubectlMigrateArguments(preflight.KubectlContext!, kubernetesNamespace, DeployOperate.PsqlHistoryCommand);
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
        arguments = DeployOperate.BuildComposeMigrateArguments(choice.Project, DeployOperate.PsqlScriptCommand(command.Verbose));
        historyArguments = DeployOperate.BuildComposeMigrateArguments(choice.Project, DeployOperate.PsqlHistoryCommand);
        against = $"Compose project {choice.Project.Name}";
      }

      Terminal.WriteLine($"{Verb} → {preflight.Target.Name}");
      Terminal.WriteLine($"  {preflight.Detail}");
      Terminal.WriteLine($"  Script:  {artifact}");
      Terminal.WriteLine($"  Runs:    {DeployOperate.BuildPipedCommandDisplay(artifact, executable, arguments)}");
      if (!Confirmed(command.Yes)) return Unit.Value;

      string[]? before = await ReadMigrationHistoryAsync(executable, historyArguments, preflight.RepoRoot, ct);
      CommandOutput result = await Shell.Builder(executable)
        .WithArguments(arguments)
        .WithStandardInput(await File.ReadAllTextAsync(artifact, ct))
        .WithWorkingDirectory(preflight.RepoRoot)
        .WithNoValidation()
        .PassthroughAsync(ct);
      string[]? after = await ReadMigrationHistoryAsync(executable, historyArguments, preflight.RepoRoot, ct);

      Report(DeployOperate.BuildMigrationReport(before, after, result.ExitCode), result.ExitCode);
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
      ProcessStep run = new(bundle, DeployOperate.BuildBundleArguments(connectionString, command.Verbose), Capture: true);
      ProcessStep delete = new("az", DeployOperate.BuildFirewallRuleDeleteArguments(group, server, subscription));

      Terminal.WriteLine($"{Verb} → aca");
      Terminal.WriteLine($"  {preflight.Detail}");
      Terminal.WriteLine($"  {resourceGroup.Detail}");
      Terminal.WriteLine($"  Server:  {server} (connection string from Key Vault {vault}, not shown)");
      Terminal.WriteLine($"  Bundle:  {bundle}");
      Terminal.WriteLine($"  Firewall rule {DeployOperate.FirewallRuleName} for {clientIp} is created for the run and always deleted afterwards.");
      if (!Confirmed(command.Yes)) return;

      AcaMigrationResult result = await DeployOperate.RunAcaBundleAsync(
        create, run, delete, (step, token) => RunStepAsync(step, token),
        reason => Terminal.WriteErrorLine(DeployOperate.FirewallCleanupFailedMessage(group, server, subscription, reason).Red()),
        ct);

      Report(DeployOperate.BuildBundleReport(BundleOutput, result.ExitCode), result.ExitCode);
      Summarize(AspireDeploy.ContainerApps, "the web-migrations bundle", $"server {server} in {group}", result.ExitCode);
    }

    private async Task MigrateNeonAsync(DeployMigrateCommand command, DeployPreflight preflight, string bundle, CancellationToken ct)
    {
      ResolvedDeployParameter? direct = preflight.Secrets?.FirstOrDefault(secret =>
        string.Equals(secret.Name, AspireDeploy.NeonDirectConnectionStringParameter, StringComparison.Ordinal));
      if (direct is null)
      {
        Fail($"Missing the direct Neon connection string ({AspireDeploy.NeonDirectConnectionStringParameter}). "
          + "Set it in the AppHost user secrets or a Parameters__ environment variable, then re-run.");
        return;
      }

      ProcessStep run = new(bundle, DeployOperate.BuildBundleArguments(direct.Value, command.Verbose), Capture: true);

      Terminal.WriteLine($"{Verb} → {AspireDeploy.ContainerAppsNeon.Name}");
      Terminal.WriteLine($"  {preflight.Detail}");
      Terminal.WriteLine("  Connection: direct Neon connection string (not shown; no firewall rule)");
      Terminal.WriteLine($"  Bundle:  {bundle}");
      if (!Confirmed(command.Yes)) return;

      int exitCode = await RunStepAsync(run, ct, DeployOperate.NeonDirectConnectionPlaceholder);
      Report(DeployOperate.BuildBundleReport(BundleOutput, exitCode), exitCode);
      Summarize(AspireDeploy.ContainerAppsNeon, "the web-migrations bundle", "the direct Neon connection", exitCode);
    }

    private async Task<int> RunStepAsync(ProcessStep step, CancellationToken ct, string connectionPlaceholder = DeployOperate.KeyVaultConnectionPlaceholder)
    {
      Terminal.WriteLine($"\n{step.Executable} {string.Join(' ', DeployOperate.RedactConnection(step.Arguments, connectionPlaceholder).Select(DeployOperate.PwshQuote))}");
      ShellBuilder builder = Shell.Builder(step.Executable)
        .WithArguments([.. step.Arguments])
        .WithNoValidation();
      if (!step.Capture)
      {
        return (await builder.PassthroughAsync(ct)).ExitCode;
      }

      CommandOutput output = await builder.RunAndCaptureAsync(ct);
      BundleOutput = output.Stdout;
      return output.ExitCode;
    }

    /// <summary>__EFMigrationsHistory through the run's own exec path; null when it cannot be read (empty on a fresh database).</summary>
    private static async Task<string[]?> ReadMigrationHistoryAsync(string executable, string[] arguments, string repoRoot, CancellationToken ct)
    {
      CommandOutput output = await Shell.Builder(executable)
        .WithArguments(arguments)
        .WithWorkingDirectory(repoRoot)
        .WithTimeout(AspireDeploy.ProbeTimeout)
        .WithNoValidation()
        .CaptureAsync(ct);
      return DeployOperate.ParseMigrationHistory(output.ExitCode, output.Stdout, output.Stderr);
    }

    /// <summary>
    /// Compares the source migration ids with <paramref name="script"/>'s and warns when the script lacks some; false
    /// (after printing the refusal) when --yes without --allow-stale must not apply a stale script.
    /// </summary>
    private async Task<bool> CheckStaleAsync(DeployMigrateCommand command, DeployPreflight preflight, string script, CancellationToken ct)
    {
      string[] folders = DeployOperate.FindSourceMigrationFolders(preflight.AppHostProject, preflight.RepoRoot);
      if (folders.Length == 0) return true;

      if (!File.Exists(script))
      {
        Terminal.WriteLine(DeployOperate.BundleStaleCheckUnavailable);
        return true;
      }

      string[] missing = DeployOperate.MigrationsMissingFromScript(
        DeployOperate.ParseSourceMigrationIds(folders.SelectMany(Directory.EnumerateFiles)),
        DeployOperate.ParseScriptMigrationIds(await File.ReadAllTextAsync(script, ct)));
      if (missing.Length == 0) return true;

      Stale = true;
      Terminal.WriteErrorLine(DeployOperate.StaleScriptWarning(preflight.Target, missing).Yellow());
      if (DeployOperate.RefuseStaleUnattended(Stale, command.Yes, command.AllowStale))
      {
        Fail(DeployOperate.StaleUnattendedRefusal(preflight.Target));
        return false;
      }

      return true;
    }

    private void Report(string[] lines, int exitCode)
    {
      if (Stale && exitCode == 0) lines = DeployOperate.QualifyRelativeToPublishedScript(lines);
      Terminal.WriteLine("");
      foreach (string line in lines)
      {
        if (exitCode == 0) Terminal.WriteLine(line);
        else Terminal.WriteErrorLine(line.Red());
      }
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
        Terminal.WriteLine(line.Green());
        return;
      }

      Terminal.WriteErrorLine(line.Red());
      Environment.ExitCode = exitCode;
    }

    private void Fail(string message)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = 1;
    }
  }
}
