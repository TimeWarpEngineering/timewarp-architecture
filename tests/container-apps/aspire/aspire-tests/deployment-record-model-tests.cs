#region Purpose
// AppHost model inspection (task 070-006): `dev deploy` / `dev deprovision` name the same compute
// environments the AppHost declares per Publish:Target, and find the deployment record where Aspire
// writes it.
#endregion

#region Design
// Model-only, like PostgresVolumeModel_Given_: DistributedApplicationTestingBuilder.CreateAsync runs
// the AppHost entry point up to Build in publish mode — nothing is built, published or deployed.
// aspire-deploy.cs is compile-linked from the dev CLI (it cannot reference the AppHost), so its
// duplicated environment names and its state-path hash are checked against Aspire itself: a renamed
// environment or a changed hashing scheme fails here instead of `dev deprovision` silently finding no
// record. The csproj defines DEV_CLI_SOURCE only where the dev CLI source exists.
#endregion

namespace Aspire.Tests;
#if DEV_CLI_SOURCE

using DevCli.Services;

[TestTag("Integration")]
public class DeploymentRecordModel_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DeploymentRecordModel_Given_>();

  public static async Task ComposeTarget_Should_DeclareTheEnvironmentDevDeployRecords() =>
    await AssertEnvironmentAsync(AspireDeploy.Compose);

  public static async Task KubernetesTarget_Should_DeclareTheEnvironmentDevDeployRecords() =>
    await AssertEnvironmentAsync(AspireDeploy.Kubernetes);

  public static async Task StateHash_Should_MatchAspireDeploymentStatePath()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--Publish:Target=compose"],
        (_, settings) => settings.EnvironmentName = "Production");

    string appHostPath = appHost.Configuration["AppHost:Path"].ShouldNotBeNull();
    AspireDeploy.AppHostPath(Path.Combine(Projects.aspire_app_host.ProjectPath, "aspire-app-host.csproj")).ShouldBe(appHostPath);
    AspireDeploy.DeploymentStateHash(appHostPath)
      .ShouldBe(appHost.Configuration["AppHost:DeploymentStatePathSha256"].ShouldNotBeNull());
    AspireDeploy.ComposeProjectName(appHostPath)
      .ShouldBe($"aspire-compose-{appHost.Configuration["AppHost:PathSha256"].ShouldNotBeNull()[..8].ToLowerInvariant()}");
  }

  private static async Task AssertEnvironmentAsync(DeployTarget target)
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", $"--Publish:Target={target.Name}"],
        (_, settings) => settings.EnvironmentName = "Production");

    appHost.Resources.OfType<IComputeEnvironmentResource>().Select(resource => resource.Name)
      .ShouldContain(target.EnvironmentResourceName);
  }
}
#endif
