#region Purpose
// Gates `dev deploy` / `dev deprovision` target parsing: default target, case-insensitive resolution, and the unknown-target refusal.
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
    AspireDeploy.ResolveTarget("aca-neon").ShouldBe(AspireDeploy.ContainerAppsNeon);
    AspireDeploy.ResolveTarget("ACA-NEON").ShouldBe(AspireDeploy.ContainerAppsNeon);
    return Task.CompletedTask;
  }

  public static Task UnknownTarget_Should_BeRefusedWithTheValidList()
  {
    AspireDeploy.ResolveTarget("swarm").ShouldBeNull();
    string message = AspireDeploy.UnknownTargetMessage("swarm");
    message.ShouldContain("'swarm'");
    message.ShouldContain("compose, kubernetes, aca, aca-neon");
    return Task.CompletedTask;
  }
}
