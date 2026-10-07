#region Purpose
// Gates `dev deploy` / `dev deprovision` target parsing, argument building, the Helm / kubectl
// preflight refusals, the aca subscription source selection and the manual-cleanup guidance, without deploying —
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
    AspireDeploy.ResolveTarget("ACA").ShouldBe(AspireDeploy.ContainerApps);
    return Task.CompletedTask;
  }

  public static Task UnknownTarget_Should_BeRefusedWithTheValidList()
  {
    AspireDeploy.ResolveTarget("swarm").ShouldBeNull();
    string message = AspireDeploy.UnknownTargetMessage("swarm");
    message.ShouldContain("'swarm'");
    message.ShouldContain("compose, kubernetes, aca");
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
    AspireDeploy.BuildAzureAccountArguments().ShouldBe(["account", "show", "--query", "[id, name]", "--output", "tsv"]);
    return Task.CompletedTask;
  }

  public static Task AcaDeploy_Should_PassTheAcaTarget()
  {
    AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.ContainerApps, nonInteractive: false)
      .ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--", "--Publish:Target=aca"]);
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

  public static Task AzureAccount_Should_ParseIdAndName()
  {
    AzureAccount account = AspireDeploy.ParseAzureAccount(true, "00000000-1111-2222-3333-444444444444\nContoso Dev\n").ShouldNotBeNull();
    account.Id.ShouldBe("00000000-1111-2222-3333-444444444444");
    account.Name.ShouldBe("Contoso Dev");
    return Task.CompletedTask;
  }

  public static Task NoAzureLogin_Should_BeRefused()
  {
    AspireDeploy.ParseAzureAccount(false, "").ShouldBeNull();
    AspireDeploy.ParseAzureAccount(true, "Please run 'az login' to setup account.").ShouldBeNull();
    AspireDeploy.NoAzureLoginMessage.ShouldContain("az login");
    return Task.CompletedTask;
  }

  private static readonly AzureAccount CliAccount = new("00000000-1111-2222-3333-444444444444", "Contoso Dev");

  public static Task NoConfiguredSubscription_Should_FallBackToTheAzCliAndPassItToAspire()
  {
    AzureSubscriptionChoice choice = AspireDeploy.ChooseAzureSubscription(null, " ", CliAccount);
    choice.SubscriptionId.ShouldBe(CliAccount.Id);
    choice.PassToAspire.ShouldBeTrue();
    choice.Detail.ShouldContain("az account show");
    choice.Detail.ShouldContain("Contoso Dev");
    return Task.CompletedTask;
  }

  public static Task EnvironmentSubscription_Should_WinAndNotBeOverridden()
  {
    AzureSubscriptionChoice choice = AspireDeploy.ChooseAzureSubscription(
      " 99999999-1111-2222-3333-444444444444 ", "88888888-1111-2222-3333-444444444444", CliAccount);
    choice.SubscriptionId.ShouldBe("99999999-1111-2222-3333-444444444444");
    choice.PassToAspire.ShouldBeFalse();
    choice.Detail.ShouldContain("Azure__SubscriptionId environment variable");
    return Task.CompletedTask;
  }

  public static Task UserSecretSubscription_Should_BeatTheAzCliAndNotBeOverridden()
  {
    // An injected Azure__SubscriptionId env var would beat the AppHost's user secret, so the az CLI
    // value must not be passed when the operator pinned one there.
    AzureSubscriptionChoice choice = AspireDeploy.ChooseAzureSubscription(null, "88888888-1111-2222-3333-444444444444", CliAccount);
    choice.SubscriptionId.ShouldBe("88888888-1111-2222-3333-444444444444");
    choice.PassToAspire.ShouldBeFalse();
    choice.Detail.ShouldContain("user secret Azure:SubscriptionId");
    choice.Detail.ShouldNotContain("az account show");
    return Task.CompletedTask;
  }

  public static Task UserSecretsList_Should_WrapTheDotnetTool()
  {
    AspireDeploy.BuildUserSecretsListArguments("/repo/app-host.csproj")
      .ShouldBe(["user-secrets", "list", "--project", "/repo/app-host.csproj"]);
    return Task.CompletedTask;
  }

  public static Task UserSecretsOutput_Should_YieldTheKeyCaseInsensitively()
  {
    const string output = "Parameters:postgres-password = p=a ss\nazure:subscriptionid = 88888888-1111-2222-3333-444444444444\nIngress:PublicUrl = https://x\n";
    AspireDeploy.ParseUserSecret(true, output, "Azure:SubscriptionId").ShouldBe("88888888-1111-2222-3333-444444444444");
    AspireDeploy.ParseUserSecret(true, output, "Parameters:postgres-password").ShouldBe("p=a ss");
    return Task.CompletedTask;
  }

  public static Task MissingOrFailedUserSecrets_Should_YieldNull()
  {
    AspireDeploy.ParseUserSecret(true, "No secrets configured for this application.\n", "Azure:SubscriptionId").ShouldBeNull();
    AspireDeploy.ParseUserSecret(false, "Azure:SubscriptionId = 88888888-1111-2222-3333-444444444444", "Azure:SubscriptionId").ShouldBeNull();
    AspireDeploy.ParseUserSecret(true, "Azure:SubscriptionId = ", "Azure:SubscriptionId").ShouldBeNull();
    AspireDeploy.ParseUserSecret(true, "Azure:SubscriptionIdOld = 1", "Azure:SubscriptionId").ShouldBeNull();
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

  public static Task AcaCleanup_Should_PrintGroupDeleteAndTheKeyVaultPurge()
  {
    string text = string.Join('\n', AspireDeploy.BuildManualCleanupLines(AspireDeploy.ContainerApps, "docker"));

    text.ShouldContain("only knows deployments recorded on the machine");
    text.ShouldContain("az group delete --name <resource-group>");
    text.ShouldNotContain("--yes");
    text.ShouldContain("az keyvault list-deleted");
    text.ShouldContain("postgres-kv");
    text.ShouldContain("az keyvault purge --name <vault>");
    text.ShouldNotContain("docker");
    return Task.CompletedTask;
  }

  public static Task AcaDestroy_Should_PointAtTheSoftDeletedKeyVault()
  {
    string.Join('\n', AspireDeploy.BuildPostDestroyLines(AspireDeploy.ContainerApps))
      .ShouldContain("az keyvault purge --name <vault>");
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
