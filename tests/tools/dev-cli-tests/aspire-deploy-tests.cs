#region Purpose
// Gates `dev deploy` / `dev deprovision` target parsing, argument building, the Helm / kubectl
// preflight refusals and the manual-cleanup guidance, without deploying —
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

  public static Task Destroy_Should_LetAspireAskWithoutYes()
  {
    AspireDeploy.BuildDestroyArguments("/repo/app-host.csproj", AspireDeploy.Compose, yes: false)
      .ShouldBe(["destroy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--", "--Publish:Target=compose"]);
    return Task.CompletedTask;
  }

  public static Task DestroyWithYes_Should_BeNonInteractive()
  {
    AspireDeploy.BuildDestroyArguments("/repo/app-host.csproj", AspireDeploy.Kubernetes, yes: true)
      .ShouldBe(["destroy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--yes", "--non-interactive", "--", "--Publish:Target=kubernetes"]);
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

public class OperatorText_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<OperatorText_Given_>();

  public static Task ComposeCleanup_Should_PrintTheManualDownWithTheConfiguredRuntime()
  {
    string text = string.Join('\n', AspireDeploy.BuildManualCleanupLines(AspireDeploy.Compose, "podman"));

    text.ShouldContain("only knows deployments recorded on the machine");
    text.ShouldContain("podman compose ls");
    text.ShouldContain("podman compose --project-name <project> down --volumes");
    text.ShouldNotContain("docker");
    return Task.CompletedTask;
  }

  public static Task KubernetesCleanup_Should_PrintHelmUninstallAndTheClaim()
  {
    string text = string.Join('\n', AspireDeploy.BuildManualCleanupLines(AspireDeploy.Kubernetes, "docker"));

    text.ShouldContain("helm uninstall <release> --namespace <namespace>");
    text.ShouldContain("helm-release-name");
    text.ShouldContain("k8s-namespace");
    text.ShouldContain("kubectl delete pvc postgres-data --namespace <namespace>");
    return Task.CompletedTask;
  }

  public static Task KubernetesDestroy_Should_PointAtTheClaim()
  {
    string.Join('\n', AspireDeploy.BuildPostDestroyLines(AspireDeploy.Kubernetes))
      .ShouldContain("kubectl delete pvc postgres-data --namespace <namespace>");
    AspireDeploy.BuildPostDestroyLines(AspireDeploy.Compose).ShouldBeEmpty();
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
    AspireDeploy.DeployDeclined.ShouldContain("Nothing was run");
    AspireDeploy.DestroyConfirmationRefusal.ShouldContain("--yes");
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

  public static Task Pattern_Should_MatchEveryDeployInvocationShape()
  {
    string[] invocations =
    [
      "run: dev deploy --target compose --yes",
      "run: ./bin/dev deprovision --yes",
      "dotnet run tools/dev-cli/dev.cs -- deploy",
      "aspire deploy --apphost app.csproj",
      "aspire destroy --non-interactive --yes",
    ];

    foreach (string invocation in invocations)
    {
      DeployInvocation().IsMatch(invocation).ShouldBeTrue(invocation);
    }

    DeployInvocation().IsMatch("run: dev publish --target compose").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task CiWorkflowsAndDevWorkflow_Should_NeverDeploy()
  {
    string root = RepoRoot();
    string github = Path.Combine(root, ".github");
    string[] files =
    [
      .. Directory.EnumerateFiles(github, "*.yml", SearchOption.AllDirectories),
      .. Directory.EnumerateFiles(github, "*.yaml", SearchOption.AllDirectories),
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
