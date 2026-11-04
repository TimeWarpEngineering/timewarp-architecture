#region Purpose
// Gates `dev deploy` / `dev deprovision` argument building and `--Parameters:*` forwarding.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireDeploy_;

public class Arguments_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Arguments_Given_>();

  public static Task Deploy_Should_PassTheTargetToTheAppHostInProduction()
  {
    AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.Kubernetes, nonInteractive: false, [])
      .ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--", "--Publish:Target=kubernetes"]);
    return Task.CompletedTask;
  }

  public static Task DeployWithYes_Should_BeNonInteractive()
  {
    AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.Compose, nonInteractive: true, [])
      .ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--non-interactive", "--", "--Publish:Target=compose"]);
    return Task.CompletedTask;
  }

  public static Task Destroy_Should_LetAspireAskWithoutYes()
  {
    AspireDeploy.BuildDestroyArguments("/repo/app-host.csproj", AspireDeploy.Compose, yes: false, [])
      .ShouldBe(["destroy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--", "--Publish:Target=compose"]);
    return Task.CompletedTask;
  }

  public static Task DestroyWithYes_Should_BeNonInteractive()
  {
    AspireDeploy.BuildDestroyArguments("/repo/app-host.csproj", AspireDeploy.Kubernetes, yes: true, [])
      .ShouldBe(["destroy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--yes", "--non-interactive", "--", "--Publish:Target=kubernetes"]);
    return Task.CompletedTask;
  }

  public static Task DestroyParameters_Should_FollowTheTargetAfterTheSeparator()
  {
    AspireDeploy.BuildDestroyArguments("/repo/app-host.csproj", AspireDeploy.Kubernetes, yes: true, KindParameters)
      .ShouldBe(["destroy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--yes", "--non-interactive", "--",
        "--Publish:Target=kubernetes", "--Parameters:k8s-namespace=timewarp-architecture", "--Parameters:registry-endpoint=localhost:5001"]);
    return Task.CompletedTask;
  }

  public static Task Probes_Should_OnlyRead()
  {
    AspireDeployCluster.BuildHelmVersionArguments().ShouldBe(["version", "--short"]);
    AspireDeployCluster.BuildKubectlContextArguments().ShouldBe(["config", "current-context"]);
    AspireDeployAzure.BuildAzureAccountArguments().ShouldBe(["account", "show", "--query", "[id, name]", "--output", "tsv"]);
    return Task.CompletedTask;
  }

  private static readonly ResolvedDeployParameter[] KindParameters =
  [
    new("k8s-namespace", "timewarp-architecture", "AppHost user secret"),
    new("registry-endpoint", "localhost:5001", "Parameters__registry-endpoint environment variable"),
  ];

  public static Task Parameters_Should_FollowTheTargetAfterTheSeparator()
  {
    AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.Kubernetes, nonInteractive: false, KindParameters)
      .ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--",
        "--Publish:Target=kubernetes", "--Parameters:k8s-namespace=timewarp-architecture", "--Parameters:registry-endpoint=localhost:5001"]);
    return Task.CompletedTask;
  }

  public static Task ParametersWithYes_Should_BeForwardedNonInteractively()
  {
    AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.Kubernetes, nonInteractive: true, KindParameters)
      .ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--non-interactive", "--",
        "--Publish:Target=kubernetes", "--Parameters:k8s-namespace=timewarp-architecture", "--Parameters:registry-endpoint=localhost:5001"]);
    return Task.CompletedTask;
  }

  public static Task ClusterProbes_Should_OnlyRead()
  {
    AspireDeployCluster.BuildKubectlReachabilityArguments("kind-simple")
      .ShouldBe(["--context", "kind-simple", "get", "--raw", "/version", "--request-timeout=5s"]);
    AspireDeployCluster.BuildKindClustersArguments().ShouldBe(["get", "clusters"]);
    return Task.CompletedTask;
  }

  public static Task AcaDeploy_Should_PassTheAcaTarget()
  {
    AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.ContainerApps, nonInteractive: false, [])
      .ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--", "--Publish:Target=aca"]);
    return Task.CompletedTask;
  }

  public static Task AcaNeonDeploy_Should_NotForwardTheConnectionStrings()
  {
    const string pooledValue = "Host=ep-pooler.neon.tech;Password=s3cret-pooled";
    const string directValue = "Host=ep.neon.tech;Password=s3cret-direct";
    ResolvedDeployParameter[] secrets =
    [
      new(AspireDeploySecrets.NeonPooledConnectionStringParameter, pooledValue, AspireDeployParameters.UserSecretSource),
      new(AspireDeploySecrets.NeonDirectConnectionStringParameter, directValue, AspireDeployParameters.UserSecretSource),
    ];

    string[] arguments = AspireDeploy.BuildDeployArguments(
      "/repo/app-host.csproj", AspireDeploy.ContainerAppsNeon, nonInteractive: false, [], secrets);
    arguments.ShouldBe(["deploy", "--apphost", "/repo/app-host.csproj", "--environment", "Production", "--", "--Publish:Target=aca-neon"]);
    string text = string.Join('\n', arguments);
    text.ShouldNotContain(pooledValue);
    text.ShouldNotContain(directValue);
    text.ShouldNotContain("neon-pooled");
    text.ShouldNotContain("neon-direct");

    Dictionary<string, string> environment = [];
    AspireDeployEnvironment.AddSecretParameterEnvironment(environment, secrets, _ => null);
    environment.Select(pair => pair.Key).ShouldBe(
    [
      "Parameters__neon-pooled-connection-string",
      "Parameters__neon-direct-connection-string",
    ]);
    environment["Parameters__neon-pooled-connection-string"].ShouldBe(pooledValue);
    environment["Parameters__neon-direct-connection-string"].ShouldBe(directValue);

    string plan = string.Join('\n', AspireDeployText.BuildDeployPlanLines(
      "/repo/app-host.csproj", AspireDeploy.ContainerAppsNeon, "subscription: example", [], secrets));
    plan.ShouldNotContain(pooledValue);
    plan.ShouldNotContain(directValue);
    plan.ShouldNotContain("neon-pooled");
    plan.ShouldNotContain("neon-direct");

    Should.Throw<InvalidOperationException>(() =>
      AspireDeploy.BuildDeployArguments("/repo/app-host.csproj", AspireDeploy.ContainerAppsNeon, false, secrets, secrets));

    AspireDeployParameters.RequiredParameters(AspireDeploy.ContainerAppsNeon).ShouldBeEmpty();
    AspireDeploySecrets.SecretParameters(AspireDeploy.ContainerAppsNeon).ShouldBe(
      [AspireDeploySecrets.NeonPooledConnectionStringParameter, AspireDeploySecrets.NeonDirectConnectionStringParameter]);
    AspireDeploySecrets.SecretParameters(AspireDeploy.ContainerApps).ShouldBeEmpty();
    AspireDeploySecrets.SecretParameters(AspireDeploy.Compose).ShouldBeEmpty();
    AspireDeploy.IsAzureContainerApps(AspireDeploy.ContainerApps).ShouldBeTrue();
    AspireDeploy.IsAzureContainerApps(AspireDeploy.ContainerAppsNeon).ShouldBeTrue();
    AspireDeploy.IsAzureContainerApps(AspireDeploy.Compose).ShouldBeFalse();
    AspireDeploy.UsesFlexibleServer(AspireDeploy.ContainerApps).ShouldBeTrue();
    AspireDeploy.UsesFlexibleServer(AspireDeploy.ContainerAppsNeon).ShouldBeFalse();
    AspireDeploy.UsesMigrationBundle(AspireDeploy.ContainerAppsNeon).ShouldBeTrue();
    AspireDeploy.UsesMigrationBundle(AspireDeploy.Kubernetes).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task AcaNeonMigrate_Should_CheckOnlyTheDirectSecretAndSkipAzure()
  {
    AspireDeploySecrets.SecretParameters(AspireDeploy.ContainerAppsNeon, PreflightScope.Migrate)
      .ShouldBe([AspireDeploySecrets.NeonDirectConnectionStringParameter]);
    AspireDeploySecrets.SecretParameters(AspireDeploy.ContainerAppsNeon, PreflightScope.Deploy)
      .ShouldBe([AspireDeploySecrets.NeonPooledConnectionStringParameter, AspireDeploySecrets.NeonDirectConnectionStringParameter]);
    AspireDeploy.ProbesAzureAccount(AspireDeploy.ContainerAppsNeon, PreflightScope.Migrate).ShouldBeFalse();
    AspireDeploy.ProbesAzureAccount(AspireDeploy.ContainerAppsNeon, PreflightScope.Deploy).ShouldBeTrue();
    AspireDeploy.ProbesAzureAccount(AspireDeploy.ContainerAppsNeon, PreflightScope.Open).ShouldBeTrue();
    AspireDeploy.ProbesAzureAccount(AspireDeploy.ContainerApps, PreflightScope.Migrate).ShouldBeTrue();
    AspireDeploy.ProbesAzureAccount(AspireDeploy.Compose, PreflightScope.Migrate).ShouldBeFalse();

    SecretResolution missing = AspireDeploySecrets.ResolveSecrets(
      AspireDeploySecrets.SecretParameters(AspireDeploy.ContainerAppsNeon, PreflightScope.Migrate), _ => null, true, "");
    string missingText = string.Join('\n', AspireDeploySecrets.CollectSecretProblems("/repo/app-host.csproj", AspireDeploy.ContainerAppsNeon, missing, null));
    missingText.ShouldContain("neon-direct-connection-string");
    missingText.ShouldNotContain("neon-pooled-connection-string");
    return Task.CompletedTask;
  }
}
