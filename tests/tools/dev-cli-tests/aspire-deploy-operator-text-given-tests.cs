#region Purpose
// Gates the operator-facing texts, including the manual-cleanup guidance.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireDeploy_;

public class OperatorText_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<OperatorText_Given_>();

  public static Task ComposeCleanup_Should_PrintTheManualDownWithTheConfiguredRuntime()
  {
    string text = string.Join('\n', AspireDeployText.BuildManualCleanupLines(AspireDeploy.Compose, "podman"));

    text.ShouldContain("only knows deployments recorded on the machine");
    text.ShouldContain("podman compose ls");
    text.ShouldContain("podman compose --project-name <project> down --volumes");
    text.ShouldNotContain("docker");
    return Task.CompletedTask;
  }

  public static Task KubernetesCleanup_Should_PrintHelmUninstallAndTheClaim()
  {
    string text = string.Join('\n', AspireDeployText.BuildManualCleanupLines(AspireDeploy.Kubernetes, "docker"));

    text.ShouldContain("helm uninstall <release> --namespace <namespace>");
    text.ShouldContain("helm-release-name");
    text.ShouldContain("k8s-namespace");
    text.ShouldContain("kubectl delete pvc postgres-data --namespace <namespace>");
    return Task.CompletedTask;
  }

  public static Task AcaCleanup_Should_PrintGroupDeleteAndTheKeyVaultPurge()
  {
    string text = string.Join('\n', AspireDeployText.BuildManualCleanupLines(AspireDeploy.ContainerApps, "docker"));

    text.ShouldContain("only knows deployments recorded on the machine");
    text.ShouldContain("az group delete --name <resource-group>");
    text.ShouldNotContain("--yes");
    text.ShouldContain("az keyvault list-deleted");
    text.ShouldContain("postgres-kv");
    text.ShouldContain("az keyvault purge --name <vault>");
    text.ShouldNotContain("docker");
    return Task.CompletedTask;
  }

  public static Task AcaNeonCleanup_Should_DeleteTheGroupAndLeaveNeon()
  {
    string text = string.Join('\n', AspireDeployText.BuildManualCleanupLines(AspireDeploy.ContainerAppsNeon, "docker"));

    text.ShouldContain("az group delete --name <resource-group>");
    text.ShouldContain("The Neon project is not part of the resource group and is not deleted.");
    text.ShouldNotContain("keyvault");
    text.ShouldNotContain("postgres-kv");
    string.Join('\n', AspireDeployText.BuildPostDestroyLines(AspireDeploy.ContainerAppsNeon))
      .ShouldContain("The Neon project is not deleted");
    string.Join('\n', AspireDeployText.BuildPostDestroyLines(AspireDeploy.ContainerAppsNeon))
      .ShouldNotContain("keyvault");
    return Task.CompletedTask;
  }

  public static Task AcaDestroy_Should_PointAtTheSoftDeletedKeyVault()
  {
    string.Join('\n', AspireDeployText.BuildPostDestroyLines(AspireDeploy.ContainerApps))
      .ShouldContain("az keyvault purge --name <vault>");
    return Task.CompletedTask;
  }

  public static Task KubernetesDestroy_Should_PointAtTheClaim()
  {
    string.Join('\n', AspireDeployText.BuildPostDestroyLines(AspireDeploy.Kubernetes))
      .ShouldContain("kubectl delete pvc postgres-data --namespace <namespace>");
    AspireDeployText.BuildPostDestroyLines(AspireDeploy.Compose).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task DeployPlan_Should_NameTheAppHostTargetAndPreflight()
  {
    string text = string.Join('\n', AspireDeployText.BuildDeployPlanLines(
      "/repo/app-host.csproj", AspireDeploy.Kubernetes, "kubectl context: kind-local",
      [new ResolvedDeployParameter("k8s-namespace", "timewarp-architecture", "AppHost user secret")]));

    text.ShouldContain("/repo/app-host.csproj");
    text.ShouldContain("Production");
    text.ShouldContain("Publish:Target=kubernetes");
    text.ShouldContain("kubectl context: kind-local");
    text.ShouldContain("k8s-namespace=timewarp-architecture (from the AppHost user secret)");
    AspireDeployText.DeployConfirmationRefusal.ShouldContain("--yes");
    AspireDeployText.DeployDeclined.ShouldContain("Nothing was run");
    AspireDeployText.DestroyConfirmationRefusal.ShouldContain("--yes");
    return Task.CompletedTask;
  }
}
