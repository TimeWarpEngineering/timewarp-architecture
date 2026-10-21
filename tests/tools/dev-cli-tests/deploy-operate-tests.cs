#region Purpose
// Gates `dev open` / `dev deploy migrate` (task 287) without a cluster, a container runtime or Azure: option parsing, the
// per-target command construction (port-forward, exec + script, bundle + firewall rule), the LoadBalancer-vs-forward
// decision, the firewall-rule cleanup on failure, the refusal texts, the preflight scopes, and agreement with the
// AppHost's resource names and its committed ingress-port parameter.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class Options_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Options_Given_>();

  public static Task ValidPort_Should_Parse()
  {
    DeployOperate.ParsePort("9090", "--port").ShouldBe((9090, null));
    DeployOperate.ParsePort(" 65535 ", "--port").ShouldBe((65535, null));
    return Task.CompletedTask;
  }

  public static Task ForwardPort_Should_PreferOptionThenIngressPortParameter()
  {
    ResolvedDeployParameter parameter = new("ingress-port", "36371", "AppHost appsettings.json");
    DeployOperate.ChooseForwardPort("9090", parameter, "app/app.csproj").ShouldBe((9090, "--port"));
    DeployOperate.ChooseForwardPort(null, parameter, "app/app.csproj").ShouldBe((36371, "ingress-port from the AppHost appsettings.json"));
    DeployOperate.ChooseForwardPort(" ", parameter, "app/app.csproj").Port.ShouldBe(36371);
    return Task.CompletedTask;
  }

  public static Task ForwardPort_Should_RefuseWithoutOptionOrParameter()
  {
    (int? port, string detail) = DeployOperate.ChooseForwardPort(null, null, "app/app.csproj");
    port.ShouldBeNull();
    detail.ShouldContain("ingress-port is not set");
    detail.ShouldContain("--port <n>");
    detail.ShouldContain("dotnet user-secrets set 'Parameters:ingress-port'");
    return Task.CompletedTask;
  }

  public static Task ForwardPort_Should_RefuseABadIngressPortParameter()
  {
    (int? port, string detail) = DeployOperate.ChooseForwardPort(null, new ResolvedDeployParameter("ingress-port", "http", "AppHost user secret"), "a.csproj");
    port.ShouldBeNull();
    detail.ShouldContain("ingress-port from the AppHost user secret 'http' is not a TCP port");
    return Task.CompletedTask;
  }

  public static Task InvalidPort_Should_BeRefused()
  {
    foreach (string value in (string[])["0", "65536", "-1", "http", "80.5"])
    {
      (int? port, string? error) = DeployOperate.ParsePort(value, "--port");
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

    AppSettingsFile[] appSettings = [new("appsettings.json", """{ "Parameters": { "ingress-port": "36371" } }""")];
    DeployParameterResolution fromAppSettings = AspireDeploy.ResolveParameters(["ingress-port"], _ => null, true, "", appSettings);
    fromAppSettings.Resolved.Single().ShouldBe(new ResolvedDeployParameter("ingress-port", "36371", "AppHost appsettings.json"));
    AspireDeploy.ResolveParameters(["ingress-port"], environment, true, "", appSettings).Resolved.Single().Value.ShouldBe("9000");

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
    DeployOperate.BuildPortForwardArguments("kind-app", "ingress-nginx", "ingress-nginx-controller", 36371)
      .ShouldBe(["--context", "kind-app", "port-forward", "--namespace", "ingress-nginx", "service/ingress-nginx-controller", "36371:80"]);
    DeployOperate.BuildControllerServiceArguments("kind-app", "edge", "traefik")
      .ShouldBe(["--context", "kind-app", "get", "service", "traefik", "--namespace", "edge", "--output", "json"]);
    return Task.CompletedTask;
  }

  public static Task Defaults_Should_BeTheKindRecipesController()
  {
    DeployOperate.IngressControllerNamespace.ShouldBe("ingress-nginx");
    DeployOperate.IngressControllerService.ShouldBe("ingress-nginx-controller");
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
    OpenPlan ip = DeployOperate.DecideKubernetesOpen(DeployOperate.ParseLoadBalancerAddress(IpLoadBalancer), "ingress-nginx", "ingress-nginx-controller", 36371);
    ip.PortForward.ShouldBeFalse();
    ip.Url.ShouldBe("http://20.1.2.3");
    ip.Detail.ShouldContain("no port-forward");

    DeployOperate.DecideKubernetesOpen(DeployOperate.ParseLoadBalancerAddress(HostnameLoadBalancer), "n", "s", 36371).Url.ShouldBe("http://lb.example.com");
    DeployOperate.DecideKubernetesOpen("2001:db8::1", "n", "s", 36371).Url.ShouldBe("http://[2001:db8::1]");
    return Task.CompletedTask;
  }

  public static Task Migrate_Should_ExecPsqlInThePostgresStatefulSetWithStdin()
  {
    DeployOperate.BuildKubectlMigrateArguments("kind-app", "my-app", DeployOperate.PsqlScriptCommand(verbose: true)).ShouldBe(
    [
      "--context", "kind-app", "exec", "-i", "--namespace", "my-app", "statefulset/postgres-statefulset", "--", "sh", "-c",
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1",
    ]);
    DeployOperate.BuildKubectlMigrateArguments("kind-app", "my-app", DeployOperate.PsqlScriptCommand(verbose: false))[^1].ShouldBe(
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" PGOPTIONS='-c client_min_messages=warning' psql -U postgres -d postgres-db -q -v ON_ERROR_STOP=1");
    DeployOperate.BuildKubectlMigrateArguments("kind-app", "my-app", DeployOperate.PsqlHistoryCommand)[^1].ShouldBe(
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d postgres-db -At -v VERBOSITY=sqlstate "
      + "-c 'SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"'");
    DeployOperate.MigrationArtifact(AspireDeploy.Kubernetes).ShouldBe("efmigrations/web-migrations.sql");
    DeployOperate.PublishedOutputDirectory(AspireDeploy.Kubernetes).ShouldBe("artifacts/aspire-output/kubernetes");
    return Task.CompletedTask;
  }

  public static Task Refusals_Should_NameTheFix()
  {
    DeployOperate.PortInUseMessage(36371).ShouldContain("--port <n>");
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

  public static Task Port_Should_PreferOptionThenEnvThenParameter()
  {
    ResolvedDeployParameter parameter = new("ingress-port", "9200", "AppHost user secret");
    DeployOperate.ChooseComposePort("7000", "9100", parameter).ShouldBe(("7000", "--port"));
    DeployOperate.ChooseComposePort(null, "9100", parameter).Port.ShouldBe("9100");
    DeployOperate.ChooseComposePort(null, null, parameter).ShouldBe(("9200", "ingress-port from the AppHost user secret"));
    DeployOperate.ChooseComposePort(null, null, null).ShouldBe((null, null));
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
    DeployOperate.BuildComposeMigrateArguments(project, DeployOperate.PsqlQuietCommand).ShouldBe(
    [
      "compose", "--project-name", "aspire-abc", "--file", "/repo/aspire-output/docker-compose.yaml",
      "exec", "-T", "postgres", "sh", "-c",
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" PGOPTIONS='-c client_min_messages=warning' psql -U postgres -d postgres-db -q -v ON_ERROR_STOP=1",
    ]);
    DeployOperate.BuildComposeMigrateArguments(project, DeployOperate.PsqlHistoryCommand)[^1].ShouldContain("-At ");
    DeployOperate.BuildComposeMigrateArguments(project, DeployOperate.PsqlHistoryCommand)[^1].ShouldContain("FROM \"__EFMigrationsHistory\"");

    // The runtime is the executable the command chooses (ASPIRE_CONTAINER_RUNTIME); the arguments never name one.
    DeployOperate.BuildComposeMigrateArguments(project, DeployOperate.PsqlQuietCommand).ShouldNotContain("docker");
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
    DeployOperate.MigrationArtifact(AspireDeploy.ContainerAppsNeon).ShouldBe("efmigrations/web-migrations");
    return Task.CompletedTask;
  }

  public static Task PrintedArguments_Should_NeverShowTheConnectionString()
  {
    string[] printed = DeployOperate.RedactConnection(DeployOperate.BuildBundleArguments("Host=h;Password=hunter2"));
    printed.ShouldBe(["--connection", "<connection string from Key Vault>"]);
    string.Join(' ', printed).ShouldNotContain("hunter2");
    string[] neon = DeployOperate.RedactConnection(
      DeployOperate.BuildBundleArguments("Host=ep-example;Password=hunter2"), DeployOperate.NeonDirectConnectionPlaceholder);
    neon.ShouldBe(["--connection", "<direct Neon connection string>"]);
    string.Join(' ', neon).ShouldNotContain("hunter2");
    string.Join(' ', neon).ShouldNotContain("ep-example");
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
      DeployOperate.BuildKubectlMigrateArguments("kind-x", "ns", DeployOperate.PsqlQuietCommand));
    display.ShouldStartWith("Get-Content -Raw /repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql | kubectl --context kind-x exec -i");
    display.ShouldEndWith("-- sh -c 'PGPASSWORD=\"$POSTGRES_PASSWORD\" PGOPTIONS=''-c client_min_messages=warning'' psql -U postgres -d postgres-db -q -v ON_ERROR_STOP=1'");
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
  private static readonly ProcessStep Bundle = new("bundle", ["--connection", "x"], Capture: true);
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
    Task<int> Run(ProcessStep step, CancellationToken _)
    {
      log.Add(step.Arguments[0]);
      return step == Bundle ? throw new OperationCanceledException("Ctrl+C") : Task.FromResult(0);
    }

    await Should.ThrowAsync<OperationCanceledException>(() => DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, Run, NoReport, CancellationToken.None));
    log.ShouldBe(["create", "--connection", "delete"]);
  }

  public static async Task CancelledToken_Should_NotCancelTheDelete()
  {
    using CancellationTokenSource cancelled = new();
    await cancelled.CancelAsync();
    CancellationToken deleteToken = default;
    Task<int> Run(ProcessStep step, CancellationToken token)
    {
      if (step == Delete)
      {
        deleteToken = token;
      }

      return Task.FromResult(step == Bundle ? 130 : 0);
    }

    await DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, Run, NoReport, cancelled.Token);
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
    static Task<int> Run(ProcessStep step, CancellationToken _)
    {
      return step == Bundle ? throw new OperationCanceledException("Ctrl+C") : Task.FromResult(step == Delete ? 1 : 0);
    }

    await Should.ThrowAsync<OperationCanceledException>(
      () => DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, Run, reported.Add, CancellationToken.None));
    reported.ShouldBe(["exit 1"]);
  }

  public static async Task ThrowingDelete_Should_BeReportedNotSwallowed()
  {
    static Task<int> Run(ProcessStep step, CancellationToken _)
    {
      return step == Delete ? throw new InvalidOperationException("az vanished") : Task.FromResult(0);
    }

    AcaMigrationResult result = await DeployOperate.RunAcaBundleAsync(Create, Bundle, Delete, Run, NoReport, CancellationToken.None);
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
    message.ShouldContain("Publishing them now");
    message.ShouldEndWith("dev publish kubernetes");
    message.ShouldNotContain("never publishes");
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

  public static Task History_Should_ReadTheIdsAndTreatAMissingTableAsNoneApplied()
  {
    DeployOperate.ParseMigrationHistory(0, "20260101_Init\n20260201_Todo\n", "").ShouldBe(["20260101_Init", "20260201_Todo"]);
    DeployOperate.ParseMigrationHistory(0, "", "").ShouldBeEmpty();
    DeployOperate.ParseMigrationHistory(1, "", "ERROR:  42P01\n").ShouldNotBeNull().ShouldBeEmpty();
    DeployOperate.ParseMigrationHistory(2, "", "psql: error: connection to server failed").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task NewlyApplied_Should_BeTheAfterIdsNotInBefore()
  {
    DeployOperate.NewlyAppliedMigrations(["a", "b"], ["a", "b", "c", "d"]).ShouldBe(["c", "d"]);
    DeployOperate.NewlyAppliedMigrations(["a", "b"], ["a", "b"]).ShouldBeEmpty();
    DeployOperate.NewlyAppliedMigrations([], ["a"]).ShouldBe(["a"]);
    return Task.CompletedTask;
  }

  public static Task Report_Should_ListTheAppliedMigrations()
  {
    DeployOperate.BuildMigrationReport(["20260101_Init"], ["20260101_Init", "20260201_Todo", "20260301_Roles"], 0).ShouldBe(
    [
      "Applied 2 migration(s):",
      "  20260201_Todo",
      "  20260301_Roles",
      "Already applied: 1 · Database now at 20260301_Roles",
    ]);
    return Task.CompletedTask;
  }

  public static Task Report_Should_SayUpToDateWhenNothingApplied()
  {
    DeployOperate.BuildMigrationReport(["20260101_Init", "20260201_Todo"], ["20260101_Init", "20260201_Todo"], 0)
      .ShouldBe(["Up to date: no pending migrations (2 applied, latest 20260201_Todo)"]);
    return Task.CompletedTask;
  }

  public static Task Report_Should_ApplyEverythingOnAFreshDatabase()
  {
    string[]? fresh = DeployOperate.ParseMigrationHistory(1, "", "ERROR:  42P01");
    DeployOperate.BuildMigrationReport(fresh, ["20260101_Init", "20260201_Todo"], 0).ShouldBe(
    [
      "Applied 2 migration(s):",
      "  20260101_Init",
      "  20260201_Todo",
      "Already applied: 0 · Database now at 20260201_Todo",
    ]);
    return Task.CompletedTask;
  }

  public static Task Report_Should_SayWhereAFailureStopped()
  {
    DeployOperate.BuildMigrationReport(["a"], ["a", "b"], 3).ShouldBe(
    [
      "Applied 1 migration(s):",
      "  b",
      "Failed while applying migrations after b",
    ]);
    DeployOperate.BuildMigrationReport(["a"], ["a"], 3).ShouldBe(["Failed while applying migrations after a"]);
    DeployOperate.BuildMigrationReport([], [], 3).ShouldBe(["Failed before any migration was applied"]);
    DeployOperate.BuildMigrationReport(["a"], null, 3).Single().ShouldContain("could not be read after the failure");
    DeployOperate.BuildMigrationReport(null, ["a"], 0).Single().ShouldContain("could not be read before the run");
    return Task.CompletedTask;
  }

  public static Task BundleReport_Should_ComeFromTheBundlesApplyingLines()
  {
    const string applied = "Build started...\nApplying migration '20260201_Todo'.\nApplying migration '20260301_Roles'.\nDone.\n";
    DeployOperate.BuildBundleReport(applied, 0).ShouldBe(
    [
      "Applied 2 migration(s):",
      "  20260201_Todo",
      "  20260301_Roles",
      "Database now at 20260301_Roles",
    ]);
    DeployOperate.BuildBundleReport("No migrations were applied. The database is already up to date.\nDone.\n", 0)
      .ShouldBe(["Up to date: no pending migrations"]);
    DeployOperate.BuildBundleReport(applied, 1).ShouldBe(
    [
      "Applied 1 migration(s):",
      "  20260201_Todo",
      "Failed while applying 20260301_Roles",
    ]);
    DeployOperate.BuildBundleReport("Applying migration '20260201_Todo'.\n", 1).ShouldBe(["Failed while applying 20260201_Todo"]);
    DeployOperate.BuildBundleReport("error\n", 1).ShouldBe(["Failed before any migration was applied"]);
    DeployOperate.BuildBundleArguments("cs", verbose: true).ShouldBe(["--connection", "cs", "--verbose"]);
    DeployOperate.BuildBundleArguments("cs").ShouldBe(["--connection", "cs"]);
    return Task.CompletedTask;
  }
}

public class StaleScript_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StaleScript_Given_>();

  // EF's idempotent script output for two migrations (Npgsql provider).
  private const string Script = """
    CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
        "MigrationId" character varying(150) NOT NULL,
        "ProductVersion" character varying(32) NOT NULL,
        CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
    );

    START TRANSACTION;
    DO $EF$
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804154245_InitialPostgresModel') THEN
        CREATE TABLE "Todo" ("Id" uuid NOT NULL);
        END IF;
    END $EF$;

    DO $EF$
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804154245_InitialPostgresModel') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260804154245_InitialPostgresModel', '11.0.0');
        END IF;
    END $EF$;

    DO $EF$
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923180147_AddCredentialAccountHint') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260923180147_AddCredentialAccountHint', '11.0.0');
        END IF;
    END $EF$;
    COMMIT;
    """;

  private static readonly string[] Folder =
  [
    "20260804154245_InitialPostgresModel.cs",
    "20260804154245_InitialPostgresModel.Designer.cs",
    "20260923180147_AddCredentialAccountHint.cs",
    "20260923180147_AddCredentialAccountHint.Designer.cs",
    "20261010075816_AddFeedbackAttachments.cs",
    "20261010075816_AddFeedbackAttachments.Designer.cs",
    "PostgresDbContextModelSnapshot.cs",
  ];

  public static Task ScriptIds_Should_ComeFromTheHistoryInserts()
  {
    DeployOperate.ParseScriptMigrationIds(Script)
      .ShouldBe(["20260804154245_InitialPostgresModel", "20260923180147_AddCredentialAccountHint"]);
    DeployOperate.ParseScriptMigrationIds("").ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task SourceIds_Should_SkipDesignerFilesAndTheSnapshot()
  {
    DeployOperate.ParseSourceMigrationIds(Folder).ShouldBe(
    [
      "20260804154245_InitialPostgresModel",
      "20260923180147_AddCredentialAccountHint",
      "20261010075816_AddFeedbackAttachments",
    ]);
    return Task.CompletedTask;
  }

  public static Task Stale_Should_ListTheSourceIdsTheScriptLacks()
  {
    string[] missing = DeployOperate.MigrationsMissingFromScript(
      DeployOperate.ParseSourceMigrationIds(Folder), DeployOperate.ParseScriptMigrationIds(Script));
    missing.ShouldBe(["20261010075816_AddFeedbackAttachments"]);
    string warning = DeployOperate.StaleScriptWarning(AspireDeploy.Kubernetes, missing);
    warning.ShouldStartWith("⚠ Published script is stale: source has 1 migration(s) the script does not");
    warning.ShouldContain("(20261010075816_AddFeedbackAttachments)");
    warning.ShouldContain("Run `dev publish kubernetes` first");
    warning.ShouldContain("then redeploy so the app and schema match.");
    return Task.CompletedTask;
  }

  public static Task Current_Should_HaveNothingMissing()
  {
    DeployOperate.MigrationsMissingFromScript(DeployOperate.ParseSourceMigrationIds(Folder[..4]), DeployOperate.ParseScriptMigrationIds(Script))
      .ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Yes_Should_RefuseAStaleScriptUnlessAllowStale()
  {
    DeployOperate.RefuseStaleUnattended(stale: true, yes: true, allowStale: false).ShouldBeTrue();
    DeployOperate.RefuseStaleUnattended(stale: true, yes: true, allowStale: true).ShouldBeFalse();
    DeployOperate.RefuseStaleUnattended(stale: true, yes: false, allowStale: false).ShouldBeFalse();
    DeployOperate.RefuseStaleUnattended(stale: false, yes: true, allowStale: false).ShouldBeFalse();
    string refusal = DeployOperate.StaleUnattendedRefusal(AspireDeploy.Compose);
    refusal.ShouldContain("dev publish compose");
    refusal.ShouldContain("--allow-stale");
    return Task.CompletedTask;
  }

  public static Task Report_Should_SayRelativeToThePublishedScript()
  {
    DeployOperate.QualifyRelativeToPublishedScript(["Up to date: no pending migrations (2 applied, latest b)"])
      .ShouldBe(["Up to date relative to the published script: no pending migrations (2 applied, latest b)"]);
    DeployOperate.QualifyRelativeToPublishedScript(["Applied 1 migration(s):", "  b", "Already applied: 1 · Database now at b"])
      .ShouldBe(["Applied 1 migration(s) relative to the published script:", "  b", "Already applied: 1 · Database now at b"]);
    return Task.CompletedTask;
  }

  public static Task SourceFolders_Should_ComeFromTheAppHostsSiblingApps()
  {
    string root = Path.Combine(Path.GetTempPath(), "stale-script-" + Guid.NewGuid().ToString("N"));
    try
    {
      string appHost = Path.Combine(root, "source", "container-apps", "aspire", "projects", "aspire-app-host", "aspire-app-host.csproj");
      Directory.CreateDirectory(Path.GetDirectoryName(appHost)!);
      File.WriteAllText(appHost, "");
      Directory.CreateDirectory(Path.Combine(root, "source", "container-apps", "web", "platform"));

      // No postgres flag: no migrations folder, so the check is skipped.
      DeployOperate.FindSourceMigrationFolders(appHost, root).ShouldBeEmpty();

      string migrations = Path.Combine(root, "source", "container-apps", "web", DeployOperate.SourceMigrationsSubpath);
      Directory.CreateDirectory(migrations);
      DeployOperate.FindSourceMigrationFolders(appHost, root).ShouldBe([migrations]);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

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
    DeployOperate.NoBrowserMessage("http://localhost:36371").ShouldContain("open http://localhost:36371");
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
    constants["PasskeyRpHostParameterName"].ShouldBe(AspireDeploy.PasskeyRpHostParameter);
    DeployOperate.PostgresStatefulSet.ShouldBe($"statefulset/{constants["PostgresResourceName"]}-statefulset");
    return Task.CompletedTask;
  }

  public static Task IngressPort_Should_BeTheCommittedParameterNotACodeLiteral()
  {
    string appHost = AppHostDirectory();
    string? committed = AspireDeploy.ParseAppSettingsParameter(
      File.ReadAllText(Path.Combine(appHost, AspireDeploy.AppSettingsFileName)), DeployOperate.IngressPortParameter);
    committed.ShouldNotBeNull();
    DeployOperate.ParsePort(committed, "Parameters:ingress-port").Error.ShouldBeNull();

    // program.cs declares the parameter from configuration (the value appsettings.json commits), never a literal port.
    string program = File.ReadAllText(Path.Combine(appHost, "program.cs"));
    program.ShouldContain("builder.Configuration[$\"Parameters:{IngressPortParameterName}\"]");
    IngressPortLiteralPattern().IsMatch(program).ShouldBeFalse("program.cs passes a literal to AddParameter(IngressPortParameterName, …)");
    program.ShouldContain("WebAuthnOptions__AllowedRpIds__");
    program.ShouldContain("ContainerAppDomain");
    program.ShouldContain("PasskeyRpHostParameterName");
    return Task.CompletedTask;
  }

  public static Task SourceMigrations_Should_BeTheFolderTheAppHostTellsEfToWriteTo()
  {
    string appHost = AppHostDirectory();
    string repoRoot = Path.GetFullPath(Path.Combine(appHost, "..", "..", "..", "..", ".."));
    string[] folders = DeployOperate.FindSourceMigrationFolders(Path.Combine(appHost, "aspire-app-host.csproj"), repoRoot);

    // program.cs: WithMigrationsProject(web-infrastructure) + WithMigrationOutputDirectory("../../platform/postgres/migrations").
    string program = File.ReadAllText(Path.Combine(appHost, "program.cs"));
    program.ShouldContain(".WithMigrationOutputDirectory(\"../../platform/postgres/migrations\")");
    folders.ShouldBe([Path.GetFullPath(Path.Combine(appHost, "..", "..", "..", "web", DeployOperate.SourceMigrationsSubpath))]);
    DeployOperate.ParseSourceMigrationIds(Directory.EnumerateFiles(folders[0])).ShouldNotBeEmpty();
    return Task.CompletedTask;
  }

  [System.Text.RegularExpressions.GeneratedRegex(@"AddParameter\(\s*IngressPortParameterName\s*,\s*""")]
  private static partial System.Text.RegularExpressions.Regex IngressPortLiteralPattern();

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
