#region Purpose
// Locks the one-command deploy order and the stop-on-failure text without running Aspire.
#endregion

namespace DeployPipeline_;

public class Steps_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Steps_Given_>();

  public static Task Database_Should_PublishDeployMigrateThenSmoke()
  {
    DeployPipeline.Steps(hasDatabase: true).ShouldBe(["publish", "deploy", "migrate", "smoke"]);
    return Task.CompletedTask;
  }

  public static Task NoDatabase_Should_SkipMigrate()
  {
    DeployPipeline.Steps(hasDatabase: false).ShouldBe(["publish", "deploy", "smoke"]);
    string message = DeployPipeline.SkipMigrateMessage(AspireDeploy.Compose);
    message.ShouldContain("compose");
    message.ShouldContain("no database");
    message.ShouldContain("migrate is skipped");
    return Task.CompletedTask;
  }
}

public class Stop_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Stop_Given_>();

  public static Task PublishFailure_Should_NamePublishAndNothingElse()
  {
    string message = DeployPipeline.FailureMessage(DeployPipeline.Publish, AspireDeploy.ContainerAppsNeon, 2);
    message.ShouldContain("exit 2");
    message.ShouldContain("Nothing else ran");
    message.ShouldContain("dev publish aca-neon");
    message.ShouldNotContain("dev deploy migrate");
    message.ShouldNotContain("dev deploy --target");
    return Task.CompletedTask;
  }

  public static Task DeployFailure_Should_NameDeployAfterPublishFinished()
  {
    string? message = DeployPipeline.StopMessage(AspireDeploy.ContainerAppsNeon,
    [
      (DeployPipeline.Publish, 0),
      (DeployPipeline.Deploy, 3),
      (DeployPipeline.Migrate, 9),
    ]);
    message.ShouldNotBeNull();
    message.ShouldContain("exit 3");
    message.ShouldContain("Publish already finished");
    message.ShouldContain("dev deploy --target aca-neon");
    message.ShouldNotContain("dev deploy migrate");
    message.ShouldNotContain("exit 9");
    return Task.CompletedTask;
  }

  public static Task MigrateFailure_Should_NameMigrateOnly()
  {
    string message = DeployPipeline.FailureMessage(DeployPipeline.Migrate, AspireDeploy.Kubernetes, 4);
    message.ShouldContain("exit 4");
    message.ShouldContain("Publish and deploy already finished");
    message.ShouldContain("dev deploy migrate --target kubernetes");
    return Task.CompletedTask;
  }

  public static Task SmokeFailure_Should_SayTheDeploymentIsUp()
  {
    string message = DeployPipeline.FailureMessage(DeployPipeline.Smoke, AspireDeploy.ContainerApps, 1);
    message.ShouldContain("The deployment is up");
    message.ShouldContain("/web-server/alive");
    message.ShouldContain("/api/identity/entra/offered");
    message.ShouldNotContain("/api/health");
    message.ShouldContain("dev deploy --target aca");
    return Task.CompletedTask;
  }

  public static Task Success_Should_NotStop()
  {
    DeployPipeline.StopMessage(AspireDeploy.Compose,
    [
      (DeployPipeline.Publish, 0),
      (DeployPipeline.Deploy, 0),
      (DeployPipeline.Smoke, 0),
    ]).ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class Smoke_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Smoke_Given_>();

  public static Task Paths_Should_BeTheAnonymousChecksAndNotHealth()
  {
    DeployPipeline.SmokePaths.ShouldBe(["/web-server/alive", "/api/identity/entra/offered"]);
    string.Join(' ', DeployPipeline.SmokePaths).ShouldNotContain("/api/health");
    return Task.CompletedTask;
  }

  public static Task Retry_Should_CoverAColdStartAndNotAClientError()
  {
    DeployPipeline.ShouldRetrySmoke(null).ShouldBeTrue();
    DeployPipeline.ShouldRetrySmoke(500).ShouldBeTrue();
    DeployPipeline.ShouldRetrySmoke(503).ShouldBeTrue();
    DeployPipeline.ShouldRetrySmoke(404).ShouldBeFalse();
    DeployPipeline.ShouldRetrySmoke(200).ShouldBeFalse();
    DeployPipeline.SmokeUrl("https://app.example/", "/web-server/alive").ShouldBe("https://app.example/web-server/alive");
    return Task.CompletedTask;
  }
}

public class PasskeyHost_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PasskeyHost_Given_>();

  public static Task EmptyOrDns_Should_BeAccepted()
  {
    DeployPipeline.PasskeyHostProblem(null).ShouldBeNull();
    DeployPipeline.PasskeyHostProblem("  ").ShouldBeNull();
    DeployPipeline.PasskeyHostProblem("localhost").ShouldBeNull();
    DeployPipeline.PasskeyHostProblem("app.example").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task SchemePortOrIp_Should_BeRefusedWithoutEchoingTheValue()
  {
    string? scheme = DeployPipeline.PasskeyHostProblem("https://app.example");
    scheme.ShouldNotBeNull();
    scheme.ShouldContain("bare DNS");
    scheme.ShouldNotContain("https://app.example");

    string? address = DeployPipeline.PasskeyHostProblem("10.0.0.1");
    address.ShouldNotBeNull();
    address.ShouldNotContain("10.0.0.1");

    DeployPipeline.PasskeyHostProblem("app.example:443").ShouldNotBeNull();
    return Task.CompletedTask;
  }
}
