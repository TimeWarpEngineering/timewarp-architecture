#region Purpose
// Gates the cluster / kind / registry refusals and their precedence.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireDeploy_;

public class ClusterChecks_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ClusterChecks_Given_>();

  public static Task ReachableContext_Should_Pass()
  {
    AspireDeployCluster.ValidateKubectlReachable(true, "kind-simple", "").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task UnreachableContext_Should_BeRefusedWithTheReason()
  {
    string error = AspireDeployCluster.ValidateKubectlReachable(
      false, "kind-simple", "The connection to the server 127.0.0.1:40703 was refused - did you specify the right host or port?\n").ShouldNotBeNull();
    error.ShouldContain("kind-simple does not answer");
    error.ShouldContain("127.0.0.1:40703 was refused");
    error.ShouldContain("kubectl config use-context");
    return Task.CompletedTask;
  }

  public static Task KindContext_Should_NameItsCluster()
  {
    AspireDeployCluster.KindClusterName("kind-simple").ShouldBe("simple");
    AspireDeployCluster.KindClusterName("kind-").ShouldBeNull();
    AspireDeployCluster.KindClusterName("aks-prod").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task ExistingKindCluster_Should_Pass()
  {
    AspireDeployCluster.ValidateKindCluster(true, "app\nsimple\n", "simple").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task MissingKindCluster_Should_BeRefused()
  {
    // `kind get clusters` prints "No kind clusters found." to stderr and nothing to stdout.
    string error = AspireDeployCluster.ValidateKindCluster(true, "", "simple").ShouldNotBeNull();
    error.ShouldContain("kind cluster 'simple'");
    error.ShouldContain("kubectl config delete-context kind-simple");
    AspireDeployCluster.ValidateKindCluster(true, "simple-old\n", "simple").ShouldNotBeNull();
    AspireDeployCluster.ValidateKindCluster(false, "", "simple").ShouldNotBeNull().ShouldContain("kind is not on PATH");
    return Task.CompletedTask;
  }

  public static Task RegistryEndpoint_Should_ProbeTheV2Api()
  {
    AspireDeployCluster.BuildRegistryProbeUri("localhost:5001").ShouldBe(new Uri("http://localhost:5001/v2/"));
    AspireDeployCluster.BuildRegistryProbeUri("https://registry.example.com/").ShouldBe(new Uri("https://registry.example.com/v2/"));
    AspireDeployCluster.BuildRegistryProbeUri("::nonsense").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task RegistryNotRunning_Should_BeRefusedWithThePasteReadyCommand()
  {
    AspireDeployCluster.ValidateRegistry(true, "localhost:5001", AppHost).ShouldBeNull();
    string error = AspireDeployCluster.ValidateRegistry(false, "localhost:5001", AppHost).ShouldNotBeNull();
    error.ShouldContain("registry at localhost:5001");
    error.ShouldContain("kind-registry");
    error.ShouldContain("dotnet user-secrets set 'Parameters:registry-endpoint' '<host:port>' --project '/repo/app-host.csproj'");
    error.ShouldNotContain("<apphost csproj>");
    return Task.CompletedTask;
  }

  public static Task RegistryFailure_Should_NameTheReason()
  {
    AspireDeployCluster.ValidateRegistry(false, "https://registry.local", AppHost, "The SSL connection could not be established.")
      .ShouldNotBeNull().ShouldContain("does not answer (The SSL connection could not be established.)");
    return Task.CompletedTask;
  }

  private const string AppHost = "/repo/app-host.csproj";

  private static readonly DeployParameterResolution RegistrySet =
    new([new ResolvedDeployParameter("registry-endpoint", "localhost:5001", "AppHost user secret")], []);

  public static Task Registry_Should_OnlyBeProbedWhenDeployingToKind()
  {
    AspireDeployCluster.RegistryToProbe("kind-simple", RegistrySet, deploying: true).ShouldBe("localhost:5001");
    AspireDeployCluster.RegistryToProbe("kind-simple", RegistrySet, deploying: false).ShouldBeNull();
    AspireDeployCluster.RegistryToProbe("aks-prod", RegistrySet, deploying: true).ShouldBeNull();
    AspireDeployCluster.RegistryToProbe("kind-simple", new([], ["registry-endpoint"]), deploying: true).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task HealthyKindCluster_Should_HaveNoProblems()
  {
    AspireDeployCluster.CollectClusterProblems(
        new ClusterProbeResults("kind-simple", true, "", KindProbeSucceeded: true, KindOutput: "simple\n",
          RegistryEndpoint: "localhost:5001", RegistryAnswered: true), AppHost)
      .ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task MissingKindCluster_Should_SuppressTheReachabilityError()
  {
    string[] problems = AspireDeployCluster.CollectClusterProblems(
      new ClusterProbeResults("kind-simple", false, "connection refused", KindProbeSucceeded: true, KindOutput: ""), AppHost);

    problems.Single().ShouldContain("kind cluster 'simple'");
    problems.Single().ShouldNotContain("does not answer");
    return Task.CompletedTask;
  }

  public static Task UnreachableNonKindContext_Should_ReportReachabilityOnly()
  {
    string[] problems = AspireDeployCluster.CollectClusterProblems(
      new ClusterProbeResults("aks-prod", false, "timed out after 20s", RegistryEndpoint: "localhost:5001"), AppHost);

    problems.Single().ShouldContain("aks-prod does not answer");
    problems.Single().ShouldContain("timed out after 20s");
    return Task.CompletedTask;
  }

  public static Task UnreachableKindClusterThatExists_Should_ReportReachabilityAndRegistry()
  {
    string[] problems = AspireDeployCluster.CollectClusterProblems(
      new ClusterProbeResults("kind-simple", false, "refused", KindProbeSucceeded: true, KindOutput: "simple\n",
        RegistryEndpoint: "localhost:5001", RegistryAnswered: false), AppHost);

    problems.Length.ShouldBe(2);
    problems[0].ShouldContain("kind-simple does not answer");
    problems[1].ShouldContain("registry at localhost:5001");
    return Task.CompletedTask;
  }

  public static Task UnprobedRegistry_Should_NotBeReported()
  {
    AspireDeployCluster.CollectClusterProblems(new ClusterProbeResults("kind-simple", true, "", KindProbeSucceeded: true, KindOutput: "simple\n"), AppHost)
      .ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task TimedOutKindProbe_Should_SayItTimedOut()
  {
    AspireDeployCluster.CollectClusterProblems(new ClusterProbeResults("kind-simple", false, "x", KindTimedOut: true), AppHost)
      .Single().ShouldContain("`kind get clusters` timed out after 20s");
    return Task.CompletedTask;
  }

  public static Task KubernetesProblems_Should_CollectIntoOneReport()
  {
    AspireDeployCluster.CollectKubernetesProblems(["parameters"], "helm", null, "kind-simple", ["cluster", "registry"])
      .ShouldBe(["parameters", "helm", "cluster", "registry"]);
    AspireDeployCluster.CollectKubernetesProblems([], null, null, null, []).ShouldBe([AspireDeployCluster.NoKubectlContextMessage]);
    AspireDeployCluster.CollectKubernetesProblems([], null, "context timed out", null, []).ShouldBe(["context timed out"]);
    AspireDeployCluster.CollectKubernetesProblems([], null, null, "aks-prod", []).ShouldBeEmpty();
    return Task.CompletedTask;
  }
}
