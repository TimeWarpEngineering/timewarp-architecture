#region Purpose
// Gates `dev open` / `dev deploy migrate` (task 287) without a cluster, a container runtime or Azure: option parsing, the
// per-target command construction (port-forward, exec + script, bundle + firewall rule), the LoadBalancer-vs-forward
// decision, the firewall-rule cleanup on failure, the refusal texts, the preflight scopes, and agreement with the
// AppHost's resource names and ingress-port default.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class Options_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Options_Given_>();

  public static Task NoPort_Should_UseTheFallback()
  {
    DeployOperate.ParsePort(null, DeployOperate.DefaultForwardPort).ShouldBe((8080, null));
    DeployOperate.ParsePort(" ", 8080).ShouldBe((8080, null));
    return Task.CompletedTask;
  }

  public static Task ValidPort_Should_Parse()
  {
    DeployOperate.ParsePort("9090", 8080).ShouldBe((9090, null));
    DeployOperate.ParsePort(" 65535 ", 8080).ShouldBe((65535, null));
    return Task.CompletedTask;
  }

  public static Task InvalidPort_Should_BeRefused()
  {
    foreach (string value in (string[])["0", "65536", "-1", "http", "80.5"])
    {
      (int? port, string? error) = DeployOperate.ParsePort(value, 8080);
      port.ShouldBeNull(value);
      error.ShouldNotBeNull().ShouldContain($"--port '{value}' is not a TCP port");
    }

    return Task.CompletedTask;
  }

  public static Task Targets_Should_ResolveLikeDevDeploy()
  {
    AspireDeploy.ResolveTarget(null).ShouldBe(AspireDeploy.Compose);
    AspireDeploy.ResolveTarget("KUBERNETES").ShouldBe(AspireDeploy.Kubernetes);
    AspireDeploy.ResolveTarget("swarm").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task OpenAndMigrateScopes_Should_SkipTheToolsTheyDoNotRun()
  {
    PreflightScope.Open.ShouldBe(new PreflightScope(RequireParameters: false, ProbeRegistry: false, RequireAspire: false, RequireHelm: false));
    PreflightScope.Migrate.ShouldBe(new PreflightScope(RequireParameters: true, ProbeRegistry: false, RequireAspire: false, RequireHelm: false));
    PreflightScope.Deploy.ShouldBe(new PreflightScope(RequireParameters: true, ProbeRegistry: true, RequireAspire: true, RequireHelm: true));
    PreflightScope.Deprovision.ShouldBe(new PreflightScope(RequireParameters: false, ProbeRegistry: false, RequireAspire: true, RequireHelm: true));
    return Task.CompletedTask;
  }

  public static Task ResolveNamedParameters_Should_UseTheDeployPrecedence()
  {
    Func<string, string?> environment = AspireDeploy.CaseInsensitiveEnvironment(new Dictionary<string, string> { ["parameters__ingress-port"] = "9000" });
    DeployParameterResolution fromEnvironment = AspireDeploy.ResolveParameters(["ingress-port"], environment, false, "");
    fromEnvironment.Resolved.Single().ShouldBe(new ResolvedDeployParameter("ingress-port", "9000", "Parameters__ingress-port environment variable"));

    DeployParameterResolution fromSecret = AspireDeploy.ResolveParameters(["ingress-port"], _ => null, true, "Parameters:ingress-port = 9100");
    fromSecret.Resolved.Single().Source.ShouldBe(AspireDeploy.UserSecretSource);

    AspireDeploy.ResolveParameters(["ingress-port"], _ => null, true, "").Missing.ShouldBe(["ingress-port"]);
    return Task.CompletedTask;
  }
}

public class Kubernetes_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Kubernetes_Given_>();

  private const string ClusterIpService = """{ "spec": { "type": "NodePort" }, "status": { "loadBalancer": {} } }""";
  private const string PendingLoadBalancer = """{ "spec": { "type": "LoadBalancer" }, "status": { "loadBalancer": {} } }""";
  private const string IpLoadBalancer = """{ "spec": { "type": "LoadBalancer" }, "status": { "loadBalancer": { "ingress": [ { "ip": "20.1.2.3" } ] } } }""";
  private const string HostnameLoadBalancer = """{ "spec": { "type": "LoadBalancer" }, "status": { "loadBalancer": { "ingress": [ { "hostname": "lb.example.com", "ip": "" } ] } } }""";

  public static Task PortForward_Should_TargetTheControllerServicePort80InTheContext()
  {
    DeployOperate.BuildPortForwardArguments("kind-app", "ingress-nginx", "ingress-nginx-controller", 8080)
      .ShouldBe(["--context", "kind-app", "port-forward", "--namespace", "ingress-nginx", "service/ingress-nginx-controller", "8080:80"]);
    DeployOperate.BuildControllerServiceArguments("kind-app", "edge", "traefik")
      .ShouldBe(["--context", "kind-app", "get", "service", "traefik", "--namespace", "edge", "--output", "json"]);
    return Task.CompletedTask;
  }

  public static Task Defaults_Should_BeTheKindRecipesController()
  {
    DeployOperate.IngressControllerNamespace.ShouldBe("ingress-nginx");
    DeployOperate.IngressControllerService.ShouldBe("ingress-nginx-controller");
    DeployOperate.DefaultForwardPort.ShouldBe(8080);
    return Task.CompletedTask;
  }

  public static Task NoExternalAddress_Should_Forward()
  {
    foreach (string json in (string[])[ClusterIpService, PendingLoadBalancer, "not json", "[]"])
    {
      string? address = DeployOperate.ParseLoadBalancerAddress(json);
      address.ShouldBeNull(json);
      OpenPlan plan = DeployOperate.DecideKubernetesOpen(address, "ingress-nginx", "ingress-nginx-controller", 9090);
      plan.PortForward.ShouldBeTrue();
      plan.Url.ShouldBe("http://localhost:9090");
      plan.Detail.ShouldContain("Ctrl+C");
    }

    return Task.CompletedTask;
  }

  public static Task ExternalAddress_Should_OpenItWithoutForwarding()
  {
    OpenPlan ip = DeployOperate.DecideKubernetesOpen(DeployOperate.ParseLoadBalancerAddress(IpLoadBalancer), "ingress-nginx", "ingress-nginx-controller", 8080);
    ip.PortForward.ShouldBeFalse();
    ip.Url.ShouldBe("http://20.1.2.3");
    ip.Detail.ShouldContain("no port-forward");

    DeployOperate.DecideKubernetesOpen(DeployOperate.ParseLoadBalancerAddress(HostnameLoadBalancer), "n", "s", 8080).Url.ShouldBe("http://lb.example.com");
    DeployOperate.DecideKubernetesOpen("2001:db8::1", "n", "s", 8080).Url.ShouldBe("http://[2001:db8::1]");
    return Task.CompletedTask;
  }

  public static Task Migrate_Should_ExecPsqlInThePostgresStatefulSetWithStdin()
  {
    DeployOperate.BuildKubectlMigrateArguments("kind-app", "my-app").ShouldBe(
    [
      "--context", "kind-app", "exec", "-i", "--namespace", "my-app", "statefulset/postgres-statefulset", "--", "sh", "-c",
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1",
    ]);
    DeployOperate.MigrationArtifact(AspireDeploy.Kubernetes).ShouldBe("efmigrations/web-migrations.sql");
    DeployOperate.PublishedOutputDirectory(AspireDeploy.Kubernetes).ShouldBe("artifacts/aspire-output/kubernetes");
    return Task.CompletedTask;
  }

  public static Task Refusals_Should_NameTheFix()
  {
    DeployOperate.PortInUseMessage(8080).ShouldContain("--port <n>");
    string controller = DeployOperate.ControllerServiceMissingMessage("kind-app", "ingress-nginx", "ingress-nginx-controller", "NotFound");
    controller.ShouldContain("--controller-namespace <namespace> --controller-service <service>");
    controller.ShouldContain("kind recipe");
    return Task.CompletedTask;
  }
}

public class Compose_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Compose_Given_>();

  private const string Env = "# Parameter ingress-port\nINGRESS_PORT=\n\n# Parameter postgres-password\nPOSTGRES_PASSWORD=secret\n";

  public static Task Port_Should_PreferOptionThenEnvThenParameterThenDefault()
  {
    ResolvedDeployParameter parameter = new("ingress-port", "9200", "AppHost user secret");
    DeployOperate.ChooseComposePort("7000", "9100", parameter).ShouldBe(("7000", "--port"));
    DeployOperate.ChooseComposePort(null, "9100", parameter).Port.ShouldBe("9100");
    DeployOperate.ChooseComposePort(null, null, parameter).ShouldBe(("9200", "ingress-port from the AppHost user secret"));
    DeployOperate.ChooseComposePort(null, null, null).ShouldBe(("8080", "the AppHost's ingress-port default"));
    return Task.CompletedTask;
  }

  public static Task PublishedEnv_Should_TreatTheBlankIngressPortAsUnset()
  {
    DeployOperate.ParseEnvValue(Env, "INGRESS_PORT").ShouldBeNull();
    DeployOperate.ParseEnvValue("INGRESS_PORT=\"9300\"\n", "INGRESS_PORT").ShouldBe("9300");
    DeployOperate.ParseEnvValue("# INGRESS_PORT=1\n", "INGRESS_PORT").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task ComposeLs_Should_Parse()
  {
    ComposeProject[] projects = DeployOperate.ParseComposeProjects(
      """[{"Name":"aspire-abc","Status":"running(5)","ConfigFiles":"/repo/aspire-output/docker-compose.yaml"},{"Name":"other","Status":"running(1)","ConfigFiles":"/x/a.yaml,/x/b.yaml"}]""");
    projects.Length.ShouldBe(2);
    projects[0].Name.ShouldBe("aspire-abc");
    projects[1].ConfigFiles.ShouldBe(["/x/a.yaml", "/x/b.yaml"]);
    DeployOperate.ParseComposeProjects("garbage").ShouldBeEmpty();
    DeployOperate.BuildComposeListArguments().ShouldBe(["compose", "ls", "--format", "json"]);
    return Task.CompletedTask;
  }

  public static Task ProjectChoice_Should_PreferTheNamedThenTheCheckoutsThenTheOnlyOne()
  {
    ComposeProject mine = new("aspire-abc", "running(5)", ["/repo/aspire-output/docker-compose.yaml"]);
    ComposeProject other = new("other", "running(1)", ["/elsewhere/compose.yaml"]);

    DeployOperate.ChooseComposeProject([mine, other], "other", "/repo", "podman").Project.ShouldBe(other);
    DeployOperate.ChooseComposeProject([mine, other], null, "/repo", "podman").Project.ShouldBe(mine);
    DeployOperate.ChooseComposeProject([other], null, "/repo", "podman").Project.ShouldBe(other);
    DeployOperate.ChooseComposeProject([mine], null, "/repo-two", "podman").Project.ShouldBe(mine);
    return Task.CompletedTask;
  }

  public static Task ProjectChoice_Should_RefuseWithTheCandidates()
  {
    ComposeProject first = new("one", "running(1)", ["/a/compose.yaml"]);
    ComposeProject second = new("two", "running(1)", ["/b/compose.yaml"]);

    ComposeProjectChoice none = DeployOperate.ChooseComposeProject([], null, "/repo", "podman");
    none.Project.ShouldBeNull();
    none.Refusal.ShouldNotBeNull().ShouldContain("dev deploy --target compose");
    none.Refusal.ShouldContain("podman compose ls");

    ComposeProjectChoice several = DeployOperate.ChooseComposeProject([first, second], null, "/repo", "docker");
    several.Refusal.ShouldNotBeNull().ShouldContain("--project-name <name>");
    several.Refusal.ShouldContain("one, two");

    DeployOperate.ChooseComposeProject([first], "missing", "/repo", "docker").Refusal.ShouldNotBeNull().ShouldContain("No running Compose project named 'missing'");
    return Task.CompletedTask;
  }

  public static Task Migrate_Should_ExecPsqlInThePostgresServiceThroughTheRuntime()
  {
    ComposeProject project = new("aspire-abc", "running(5)", ["/repo/aspire-output/docker-compose.yaml"]);
    DeployOperate.BuildComposeMigrateArguments(project).ShouldBe(
    [
      "compose", "--project-name", "aspire-abc", "--file", "/repo/aspire-output/docker-compose.yaml",
      "exec", "-T", "postgres", "sh", "-c", "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1",
    ]);

    // The runtime is the executable the command chooses (ASPIRE_CONTAINER_RUNTIME); the arguments never name one.
    DeployOperate.BuildComposeMigrateArguments(project).ShouldNotContain("docker");
    DeployOperate.MigrationArtifact(AspireDeploy.Compose).ShouldBe("efmigrations/web-migrations.sql");
    return Task.CompletedTask;
  }
}

public class ContainerApps_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ContainerApps_Given_>();

  private const string Subscription = "00000000-0000-0000-0000-000000000001";

  public static Task ResourceGroup_Should_PreferOptionThenEnvThenUserSecret()
  {
    DeployOperate.ChooseResourceGroup("opt", "env", "secret").Name.ShouldBe("opt");
    DeployOperate.ChooseResourceGroup(null, "env", "secret").Detail.ShouldContain("Azure__ResourceGroup environment variable");
    DeployOperate.ChooseResourceGroup(" ", null, "secret").Detail.ShouldContain("AppHost user secret Azure:ResourceGroup");
    DeployOperate.ChooseResourceGroup(null, null, null).Name.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task NoResourceGroup_Should_BeRefusedWithPwshCommands()
  {
    string message = DeployOperate.NoResourceGroupMessage("dev open", "/repo/app-host.csproj");
    message.ShouldContain("dev open --target aca --resource-group <name>");
    message.ShouldContain("${env:Azure__ResourceGroup} = '<name>'");
    message.ShouldContain("dotnet user-secrets set 'Azure:ResourceGroup' '<name>' --project '/repo/app-host.csproj'");
    return Task.CompletedTask;
  }

  public static Task Open_Should_QueryTheIngressContainerAppsFqdn()
  {
    DeployOperate.BuildIngressFqdnArguments("rg", Subscription).ShouldBe(
    [
      "containerapp", "show", "--name", "ingress", "--resource-group", "rg", "--subscription", Subscription,
      "--query", "properties.configuration.ingress.fqdn", "--output", "tsv",
    ]);
    DeployOperate.ParseSingleValue(true, "ingress.happy.eastus.azurecontainerapps.io\n").ShouldBe("ingress.happy.eastus.azurecontainerapps.io");
    DeployOperate.ParseSingleValue(false, "x").ShouldBeNull();
    DeployOperate.ParseSingleValue(true, "a\nb").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Migrate_Should_BuildTheFirewallRuleAndBundleCommands()
  {
    DeployOperate.BuildFirewallRuleCreateArguments("rg", "pg1", Subscription, "203.0.113.7").ShouldBe(
    [
      "postgres", "flexible-server", "firewall-rule", "create", "--resource-group", "rg", "--name", "pg1", "--subscription", Subscription,
      "--rule-name", "operator-migrate", "--start-ip-address", "203.0.113.7", "--end-ip-address", "203.0.113.7",
    ]);
    DeployOperate.BuildFirewallRuleDeleteArguments("rg", "pg1", Subscription).ShouldBe(
    [
      "postgres", "flexible-server", "firewall-rule", "delete", "--resource-group", "rg", "--name", "pg1", "--subscription", Subscription,
      "--rule-name", "operator-migrate", "--yes",
    ]);
    DeployOperate.BuildBundleArguments("Host=h;Password=p").ShouldBe(["--connection", "Host=h;Password=p"]);
    DeployOperate.BuildKeyVaultListArguments("rg", Subscription).ShouldContain("[?tags.\"aspire-resource-name\"=='postgres-kv'].name");
    DeployOperate.BuildConnectionStringArguments("kv1", Subscription).ShouldContain("connectionstrings--postgres-db");
    DeployOperate.MigrationArtifact(AspireDeploy.ContainerApps).ShouldBe("efmigrations/web-migrations");
    return Task.CompletedTask;
  }

  public static Task PrintedArguments_Should_NeverShowTheConnectionString()
  {
    string[] printed = DeployOperate.RedactConnection(DeployOperate.BuildBundleArguments("Host=h;Password=hunter2"));
    printed.ShouldBe(["--connection", "<connection string from Key Vault>"]);
    string.Join(' ', printed).ShouldNotContain("hunter2");
    return Task.CompletedTask;
  }

  public static Task ClientIp_Should_PreferTheOptionAndRequireIpv4()
  {
    DeployOperate.ChooseClientIp("198.51.100.4", "203.0.113.7").ShouldBe("198.51.100.4");
    DeployOperate.ChooseClientIp(null, "203.0.113.7\n").ShouldBe("203.0.113.7");
    DeployOperate.ChooseClientIp(null, "<html>").ShouldBeNull();
    DeployOperate.ChooseClientIp("2001:db8::1", null).ShouldBeNull();
    DeployOperate.NoClientIpMessage(null).ShouldContain("--client-ip <ipv4>");
    DeployOperate.NoClientIpMessage("nope").ShouldContain("not an IPv4 address");
    return Task.CompletedTask;
  }

  public static Task Refusals_Should_NameTheFix()
  {
    DeployOperate.NoFlexibleServerMessage("rg", 0).ShouldContain("dev deploy --target aca");
    DeployOperate.NoFlexibleServerMessage("rg", 2).ShouldContain("2 PostgreSQL Flexible Servers");
    string role = DeployOperate.ConnectionStringUnreadableMessage("rg", "kv1");
    role.ShouldContain("Cannot read the Key Vault secret connectionstrings--postgres-db in kv1");
    role.ShouldContain("az role assignment create --assignee (az ad signed-in-user show --query id --output tsv) --role 'Key Vault Secrets User'");
    DeployOperate.ConnectionStringUnreadableMessage("rg", null).ShouldContain("No Key Vault tagged aspire-resource-name=postgres-kv");
    // pwsh: `" escapes a double quote inside a double-quoted string; \" would end it.
    role.ShouldContain("--query \"[?tags.`\"aspire-resource-name`\"=='postgres-kv'].name\"");
    role.ShouldNotContain("\\\"");
    return Task.CompletedTask;
  }

  public static Task PrintedMigrateCommand_Should_BePwsh()
  {
    string display = DeployOperate.BuildPipedCommandDisplay(
      "/repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql",
      "kubectl",
      DeployOperate.BuildKubectlMigrateArguments("kind-x", "ns"));
    display.ShouldStartWith("Get-Content -Raw /repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql | kubectl --context kind-x exec -i");
    display.ShouldEndWith("-- sh -c 'PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1'");
    display.ShouldNotContain(" < ");
    DeployOperate.PwshQuote("it's here").ShouldBe("'it''s here'");
    DeployOperate.PwshQuote("--file").ShouldBe("--file");
    return Task.CompletedTask;
  }
}

public class FirewallCleanup_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FirewallCleanup_Given_>();

  private static readonly ProcessStep Create = new("az", ["create"]);
  private static readonly ProcessStep Bundle = new("bundle", ["--connection", "x"], Terminal: true);
  private static readonly ProcessStep Delete = new("az", ["delete"]);
  private static readonly Action<string> NoReport = _ => { };

  private static Func<ProcessStep, CancellationToken, Task<int>> Runner(List<string> log, Func<ProcessStep, int> exitCode) =>
    (step, _) =>
    {
      log.Add(step.Arguments[0]);
      return Task.FromResult(exitCode(step));
    };

  public static async Task Success_Should_CreateRunAndDelete()
  {
    List<string> log = [];
    AcaMigrationResult result = await DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, Runner(log, _ => 0), NoReport, CancellationToken.None);
    log.ShouldBe(["create", "--connection", "delete"]);
    result.ShouldBe(new AcaMigrationResult(0, 0, null));
  }

  public static async Task FailedBundle_Should_StillDeleteTheRuleAndReportTheBundlesExitCode()
  {
    List<string> log = [];
    AcaMigrationResult result = await DeployOperate.RunAcaBundleAsync(
      Create, Bundle, Delete, Runner(log, step => step == Bundle ? 3 : 0), NoReport, CancellationToken.None);
    log.ShouldBe(["create", "--connection", "delete"]);
    result.ExitCode.ShouldBe(3);
    result.CleanupFailure.ShouldBeNull();
  }

  public static async Task FailedCreate_Should_SkipTheBundleAndStillDelete()
  {
    List<string> log = [];
    AcaMigrationResult result = await DeployOperate.RunAcaBundleAsync(
      Create, Bundle, Delete, Runner(log, step => step == Create ? 2 : 0), NoReport, CancellationToken.None);
    log.ShouldBe(["create", "delete"]);
    result.ExitCode.ShouldBe(2);
  }

  public static async Task ThrowingBundle_Should_StillDeleteTheRule()
  {
    List<string> log = [];
    Func<ProcessStep, CancellationToken, Task<int>> run = (step, _) =>
    {
      log.Add(step.Arguments[0]);
      return step == Bundle ? throw new OperationCanceledException("Ctrl+C") : Task.FromResult(0);
    };

    await Should.ThrowAsync<OperationCanceledException>(() => DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, run, NoReport, CancellationToken.None));
    log.ShouldBe(["create", "--connection", "delete"]);
  }

  public static async Task CancelledToken_Should_NotCancelTheDelete()
  {
    using CancellationTokenSource cancelled = new();
    await cancelled.CancelAsync();
    CancellationToken deleteToken = default;
    Func<ProcessStep, CancellationToken, Task<int>> run = (step, token) =>
    {
      if (step == Delete)
      {
        deleteToken = token;
      }

      return Task.FromResult(step == Bundle ? 130 : 0);
    };

    await DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, run, NoReport, cancelled.Token);
    deleteToken.IsCancellationRequested.ShouldBeFalse();
  }

  public static async Task FailedDelete_Should_FailTheRunAndPrintTheCommand()
  {
    List<string> log = [];
    AcaMigrationResult result = await DeployOperate.RunAcaBundleAsync(
      Create, Bundle, Delete, Runner(log, step => step == Delete ? 1 : 0), NoReport, CancellationToken.None);
    result.ExitCode.ShouldBe(1);
    result.CleanupExitCode.ShouldBe(1);
    result.CleanupFailure.ShouldBe("exit 1");

    string message = DeployOperate.FirewallCleanupFailedMessage("rg", "pg1", "sub", "exit 1");
    message.ShouldContain("could NOT be deleted");
    message.ShouldContain("az postgres flexible-server firewall-rule delete --resource-group rg --name pg1 --subscription sub --rule-name operator-migrate --yes");
  }

  public static async Task ThrowingBundleAndFailedDelete_Should_StillReportTheCleanupFailure()
  {
    List<string> reported = [];
    Func<ProcessStep, CancellationToken, Task<int>> run = (step, _) =>
      step == Bundle ? throw new OperationCanceledException("Ctrl+C") : Task.FromResult(step == Delete ? 1 : 0);

    await Should.ThrowAsync<OperationCanceledException>(
      () => DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, run, reported.Add, CancellationToken.None));
    reported.ShouldBe(["exit 1"]);
  }

  public static async Task ThrowingDelete_Should_BeReportedNotSwallowed()
  {
    Func<ProcessStep, CancellationToken, Task<int>> run = (step, _) =>
      step == Delete ? throw new InvalidOperationException("az vanished") : Task.FromResult(0);
    AcaMigrationResult result = await DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, run, NoReport, CancellationToken.None);
    result.ExitCode.ShouldBe(1);
    result.CleanupFailure.ShouldBe("az vanished");
  }
}

public class Migrate_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Migrate_Given_>();

  public static Task MissingPublishedOutput_Should_RefuseWithTheExactPublishCommand()
  {
    string message = DeployOperate.MissingPublishedOutputMessage(AspireDeploy.Kubernetes, "/repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql");
    message.ShouldContain("/repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql does not exist");
    message.ShouldEndWith("dev publish kubernetes");
    return Task.CompletedTask;
  }

  public static Task Confirmation_Should_RefuseWithoutATerminal()
  {
    DeployOperate.MigrateConfirmationRefusal.ShouldContain("--yes");
    DeployOperate.MigrateDeclined.ShouldContain("Nothing was run");
    return Task.CompletedTask;
  }

  public static Task Summary_Should_SayWhatRanAgainstWhichTarget()
  {
    DeployOperate.BuildMigrateSummary(AspireDeploy.Kubernetes, "web-migrations.sql with psql", "context kind-app, namespace my-app", 0)
      .ShouldBe("dev deploy migrate: applied web-migrations.sql with psql to kubernetes (context kind-app, namespace my-app).");
    DeployOperate.BuildMigrateSummary(AspireDeploy.Compose, "web-migrations.sql with psql", "Compose project p", 3)
      .ShouldBe("dev deploy migrate: web-migrations.sql with psql against compose (Compose project p) failed (exit 3).");
    return Task.CompletedTask;
  }
}

public class Browser_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Browser_Given_>();

  public static Task Wsl_Should_PreferWslviewThenTheWindowsBrowser()
  {
    DeployOperate.BrowserLaunchers(isWindows: false, isMacOs: false, isWsl: true).Select(launcher => launcher.Executable)
      .ShouldBe(["wslview", "explorer.exe", "xdg-open"]);
    return Task.CompletedTask;
  }

  public static Task EachPlatform_Should_HaveAnOpener()
  {
    DeployOperate.BrowserLaunchers(true, false, false).Single().Executable.ShouldBe("explorer.exe");
    DeployOperate.BrowserLaunchers(false, true, false).Single().Executable.ShouldBe("open");
    DeployOperate.BrowserLaunchers(false, false, false).Single().Executable.ShouldBe("xdg-open");
    DeployOperate.NoBrowserMessage("http://localhost:8080").ShouldContain("open http://localhost:8080");
    return Task.CompletedTask;
  }

  public static Task WslDetection_Should_UseTheDistroOrKernel()
  {
    DeployOperate.IsWsl("Ubuntu", null).ShouldBeTrue();
    DeployOperate.IsWsl(null, "Linux version 6.18.40.1-microsoft-standard-WSL2").ShouldBeTrue();
    DeployOperate.IsWsl(null, "Linux version 6.8.0-generic").ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public partial class AppHostAgreement_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AppHostAgreement_Given_>();

  public static Task ResourceNames_Should_EqualTheAppHostConstants()
  {
    string appHost = AppHostDirectory();
    Dictionary<string, string> constants = ConstantPattern()
      .Matches(File.ReadAllText(Path.Combine(appHost, "constants.cs")))
      .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);

    constants["YarpResourceName"].ShouldBe(DeployOperate.IngressResourceName);
    constants["PostgresResourceName"].ShouldBe(DeployOperate.PostgresResourceName);
    constants["PostgresDatabaseResourceName"].ShouldBe(DeployOperate.PostgresDatabaseName);
    constants["IngressPortParameterName"].ShouldBe(DeployOperate.IngressPortParameter);
    DeployOperate.PostgresStatefulSet.ShouldBe($"statefulset/{constants["PostgresResourceName"]}-statefulset");
    return Task.CompletedTask;
  }

  public static Task IngressPortDefault_Should_EqualTheAppHostsDefault()
  {
    string program = File.ReadAllText(Path.Combine(AppHostDirectory(), "program.cs"));
    program.ShouldContain($"AddParameter(IngressPortParameterName, \"{DeployOperate.DefaultIngressPort}\"");
    return Task.CompletedTask;
  }

  [System.Text.RegularExpressions.GeneratedRegex(@"const\s+string\s+(\w+)\s*=\s*""([^""]*)""")]
  private static partial System.Text.RegularExpressions.Regex ConstantPattern();

  private static string AppHostDirectory()
  {
    for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
    {
      string candidate = Path.Combine(directory.FullName, "source", "container-apps", "aspire", "projects", "aspire-app-host");
      if (File.Exists(Path.Combine(candidate, "constants.cs")))
      {
        return candidate;
      }
    }

    throw new DirectoryNotFoundException("aspire-app-host not found above the test output directory.");
  }
}
