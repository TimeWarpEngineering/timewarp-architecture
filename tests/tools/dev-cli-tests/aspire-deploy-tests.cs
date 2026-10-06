#region Purpose
// Gates `dev deploy` / `dev deprovision` target parsing, argument building, the Helm / kubectl
// preflight refusals, the deployment-record lookup and the no-record guidance, without deploying —
// and that no CI workflow or `dev workflow` mode ever invokes a deploy.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireDeploy_;

public class Targets_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Targets_Given_>();

  public static Task NoTarget_Should_UseTheAppHostDefaultCompose()
  {
    AspireDeploy.ResolveTarget(null).ShouldBe(AspireDeploy.Compose);
    AspireDeploy.ResolveTarget(" ").ShouldBe(AspireDeploy.Compose);
    return Task.CompletedTask;
  }

  public static Task KnownTargets_Should_ResolveCaseInsensitively()
  {
    AspireDeploy.ResolveTarget("compose").ShouldBe(AspireDeploy.Compose);
    AspireDeploy.ResolveTarget("Kubernetes").ShouldBe(AspireDeploy.Kubernetes);
    return Task.CompletedTask;
  }

  public static Task UnknownTarget_Should_BeRefusedWithTheValidList()
  {
    AspireDeploy.ResolveTarget("swarm").ShouldBeNull();
    string message = AspireDeploy.UnknownTargetMessage("swarm");
    message.ShouldContain("'swarm'");
    message.ShouldContain("compose, kubernetes");
    return Task.CompletedTask;
  }
}

public class Arguments_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Arguments_Given_>();

  public static Task Deploy_Should_PassTheTargetToTheAppHostInProduction()
  {
    AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.Kubernetes, nonInteractive: false)
      .ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--", "--Publish:Target=kubernetes"]);
    return Task.CompletedTask;
  }

  public static Task DeployWithYes_Should_BeNonInteractive()
  {
    AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.Compose, nonInteractive: true)
      .ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--non-interactive", "--", "--Publish:Target=compose"]);
    return Task.CompletedTask;
  }

  public static Task Destroy_Should_TargetTheSameAppHostAndEnvironment()
  {
    AspireDeploy.BuildDestroyArguments("/repo/app-host.csproj", AspireDeploy.Compose)
      .ShouldBe(["destroy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--non-interactive", "--yes", "--", "--Publish:Target=compose"]);
    return Task.CompletedTask;
  }

  public static Task Probes_Should_OnlyRead()
  {
    AspireDeploy.BuildHelmVersionArguments().ShouldBe(["version", "--short"]);
    AspireDeploy.BuildKubectlContextArguments().ShouldBe(["config", "current-context"]);
    return Task.CompletedTask;
  }
}

public class Preflight_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Preflight_Given_>();

  public static Task Helm42OrLater_Should_Pass()
  {
    AspireDeploy.ValidateHelm(true, "v4.2.0+g1a2b3c4\n").ShouldBeNull();
    AspireDeploy.ValidateHelm(true, "v4.3.0+gbec5b06").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task OlderHelm_Should_BeRefusedWithTheInstalledVersion()
  {
    string? error = AspireDeploy.ValidateHelm(true, "v3.16.2+g13654a5");
    error.ShouldNotBeNull();
    error.ShouldContain("4.2");
    error.ShouldContain("installed: 3.16.2");
    return Task.CompletedTask;
  }

  public static Task MissingOrGarbledHelm_Should_BeRefused()
  {
    AspireDeploy.ValidateHelm(false, "").ShouldNotBeNull().ShouldContain("not found on PATH");
    AspireDeploy.ValidateHelm(true, "helm: unknown").ShouldNotBeNull().ShouldContain("Could not determine");
    return Task.CompletedTask;
  }

  public static Task KubectlContext_Should_BeReturnedTrimmed()
  {
    AspireDeploy.ParseKubectlContext(true, "kind-simple\n").ShouldBe("kind-simple");
    return Task.CompletedTask;
  }

  public static Task NoKubectlContext_Should_BeRefused()
  {
    AspireDeploy.ParseKubectlContext(false, "").ShouldBeNull();
    AspireDeploy.ParseKubectlContext(true, "   ").ShouldBeNull();
    AspireDeploy.NoKubectlContextMessage.ShouldContain("kubectl config use-context");
    return Task.CompletedTask;
  }

  public static Task ContainerRuntime_Should_HonourAspireContainerRuntime()
  {
    AspireDeploy.ContainerRuntime(null).ShouldBe("docker");
    AspireDeploy.ContainerRuntime("podman").ShouldBe("podman");
    return Task.CompletedTask;
  }
}

public class DeploymentRecord_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DeploymentRecord_Given_>();

  private const string StatePath = "/home/op/.aspire/deployments/ABC/production.json";

  // Shape of a real 13.6 state file: flattened Section:Key entries.
  private const string FlatState = """
    {
      "Parameters:postgres-password": "secret",
      "DockerCompose:compose:ComposeFilePath": "/out/docker-compose.yaml",
      "DockerCompose:compose:ProjectName": "aspire-compose-0123abcd",
      "Helm:k8s:ReleaseName": "app",
      "Helm:k8s:Namespace": "apps"
    }
    """;

  public static Task NoStateFile_Should_HaveNoRecord()
  {
    AspireDeploy.FindRecord(StatePath, null, null, AspireDeploy.Compose).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task ParametersOnly_Should_HaveNoRecord()
  {
    const string state = """{ "Parameters:postgres-password": "secret" }""";
    AspireDeploy.FindRecord(StatePath, state, null, AspireDeploy.Kubernetes).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task FlatState_Should_FindEachTargetsOwnSection()
  {
    DeploymentRecord compose = AspireDeploy.FindRecord(StatePath, FlatState, null, AspireDeploy.Compose).ShouldNotBeNull();
    compose.Values["ProjectName"].ShouldBe("aspire-compose-0123abcd");
    compose.Values.ShouldNotContainKey("ReleaseName");

    DeploymentRecord helm = AspireDeploy.FindRecord(StatePath, FlatState, null, AspireDeploy.Kubernetes).ShouldNotBeNull();
    helm.Values["Namespace"].ShouldBe("apps");
    helm.Values.Keys.ShouldNotContain(key => key.Contains("password", StringComparison.OrdinalIgnoreCase));
    return Task.CompletedTask;
  }

  public static Task NestedState_Should_AlsoBeFound()
  {
    const string state = """{ "Helm": { "k8s": { "ReleaseName": "app", "Namespace": "apps" } } }""";
    AspireDeploy.FindRecord(StatePath, state, null, AspireDeploy.Kubernetes).ShouldNotBeNull();
    return Task.CompletedTask;
  }

  public static Task MigrationCurrentState_Should_WinOverTheFile()
  {
    const string migration = """{ "CurrentState": "{\"Helm\":{\"k8s\":{\"ReleaseName\":\"migrated\"}}}", "LegacyFallbackDisabled": false }""";
    AspireDeploy.FindRecord(StatePath, FlatState, migration, AspireDeploy.Kubernetes)
      .ShouldNotBeNull().Values["ReleaseName"].ShouldBe("migrated");
    return Task.CompletedTask;
  }

  public static Task CorruptState_Should_HaveNoRecord()
  {
    AspireDeploy.FindRecord(StatePath, "{ not json", "also not json", AspireDeploy.Compose).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task StatePath_Should_MirrorAspireLayout()
  {
    const string appHostPath = "/work/app/aspire-app-host/aspire-app-host.csproj";
    string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(appHostPath)));

    AspireDeploy.AppHostPath("/work/app/aspire-app-host/aspire-app-host.csproj").ShouldBe(appHostPath);
    AspireDeploy.DeploymentStatePath("/home/op/.aspire", appHostPath)
      .ShouldBe(Path.Combine("/home/op/.aspire", "deployments", hash, "production.json"));
    AspireDeploy.DeploymentStateHash("/Work/App/aspire-app-host/aspire-app-host.csproj").ShouldBe(hash);
    AspireDeploy.ComposeProjectName(appHostPath).ShouldBe($"aspire-compose-{hash[..8].ToLowerInvariant()}");
    return Task.CompletedTask;
  }

  public static Task AspireHome_Should_OverrideTheProfileDefault()
  {
    AspireDeploy.AspireHome(null, "/home/op").ShouldBe(Path.Combine("/home/op", ".aspire"));
    AspireDeploy.AspireHome("/srv/aspire", "/home/op").ShouldBe("/srv/aspire");
    return Task.CompletedTask;
  }
}

public class OperatorText_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<OperatorText_Given_>();

  public static Task ComposeNoRecord_Should_PrintTheManualDownWithTheConfiguredRuntime()
  {
    string text = string.Join('\n', AspireDeploy.BuildNoRecordLines(
      AspireDeploy.Compose, "/state/production.json", "/work/app/aspire-app-host", "podman"));

    text.ShouldContain("No compose deployment is recorded");
    text.ShouldContain("/state/production.json");
    text.ShouldContain("nothing was run and nothing was removed");
    text.ShouldContain($"podman compose --project-name {AspireDeploy.ComposeProjectName("/work/app/aspire-app-host")} down --volumes");
    text.ShouldNotContain("docker");
    return Task.CompletedTask;
  }

  public static Task KubernetesNoRecord_Should_PrintHelmUninstallAndTheClaim()
  {
    string text = string.Join('\n', AspireDeploy.BuildNoRecordLines(
      AspireDeploy.Kubernetes, "/state/production.json", "/work/app/aspire-app-host", "docker"));

    text.ShouldContain("No kubernetes deployment is recorded");
    text.ShouldContain("helm uninstall <release> --namespace <namespace>");
    text.ShouldContain("kubectl delete pvc postgres-data --namespace <namespace>");
    return Task.CompletedTask;
  }

  public static Task DeprovisionWithoutYes_Should_ShowTheRecordAndRequireYes()
  {
    DeploymentRecord record = new("/state/production.json", new Dictionary<string, string> { ["ReleaseName"] = "app", ["Namespace"] = "apps" });
    string text = string.Join('\n', AspireDeploy.BuildDeprovisionRefusalLines(AspireDeploy.Kubernetes, record));

    text.ShouldContain("/state/production.json");
    text.ShouldContain("ReleaseName: app");
    text.ShouldContain("Namespace: apps");
    text.ShouldContain("data");
    text.ShouldContain("--yes");
    text.ShouldContain("Nothing was run");
    return Task.CompletedTask;
  }

  public static Task KubernetesDestroy_Should_PointAtTheRecordedNamespaceClaim()
  {
    DeploymentRecord record = new("/state/production.json", new Dictionary<string, string> { ["ReleaseName"] = "app", ["Namespace"] = "apps" });

    string.Join('\n', AspireDeploy.BuildPostDestroyLines(AspireDeploy.Kubernetes, record))
      .ShouldContain("kubectl delete pvc postgres-data --namespace apps");
    AspireDeploy.BuildPostDestroyLines(AspireDeploy.Compose, record).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task DeployPlan_Should_NameTheAppHostTargetAndPreflight()
  {
    string text = string.Join('\n', AspireDeploy.BuildDeployPlanLines("/repo/app-host.csproj", AspireDeploy.Kubernetes, "kubectl context: kind-local"));

    text.ShouldContain("/repo/app-host.csproj");
    text.ShouldContain("Production");
    text.ShouldContain("Publish:Target=kubernetes");
    text.ShouldContain("kubectl context: kind-local");
    AspireDeploy.DeployConfirmationRefusal.ShouldContain("--yes");
    return Task.CompletedTask;
  }
}

public partial class NeverAutomated_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<NeverAutomated_Given_>();

  // A dev deploy/deprovision verb (bin/dev or the dev.cs runfile) or a raw aspire deploy/destroy.
  [System.Text.RegularExpressions.GeneratedRegex(
    @"(\bdev(\.cs)?\s+(--\s+)?(deploy|deprovision)\b)|(\baspire\s+(deploy|destroy)\b)",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
  private static partial System.Text.RegularExpressions.Regex DeployInvocation();

  public static Task CiWorkflowsAndDevWorkflow_Should_NeverDeploy()
  {
    string root = RepoRoot();
    string[] files =
    [
      .. Directory.GetFiles(Path.Combine(root, ".github", "workflows"), "*.yml"),
      Path.Combine(root, "tools", "dev-cli", "endpoints", "workflow-command.cs"),
    ];
    files.Length.ShouldBeGreaterThan(1);

    string[] offenders =
    [
      .. files.SelectMany(file => File.ReadLines(file)
        .Select((line, index) => (line, index))
        .Where(entry => DeployInvocation().IsMatch(entry.line))
        .Select(entry => $"{Path.GetRelativePath(root, file)}:{entry.index + 1}: {entry.line.Trim()}")),
    ];

    offenders.ShouldBeEmpty("Deploying is operator-run (`dev deploy`), never a CI step or `dev workflow` mode.");
    return Task.CompletedTask;
  }

  private static string RepoRoot()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git")))
    {
      directory = directory.Parent;
    }

    return directory.ShouldNotBeNull().FullName;
  }
}
