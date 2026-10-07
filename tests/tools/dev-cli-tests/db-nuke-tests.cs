#region Purpose
// Gates `dev db nuke` argument building, the --yes refusal text, the pre-13.6-volume hint and the
// shared Aspire CLI version guard, without running Aspire.
#endregion

// ReSharper disable InconsistentNaming
namespace DbNuke_;

public class BuildArguments_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BuildArguments_Given_>();

  public static Task Stop_Should_ForceRemoveVolumesForThisAppHostOnly()
  {
    DbNuke.BuildStopArguments("/repo/app-host.csproj")
      .ShouldBe(["stop", "--apphost", "/repo/app-host.csproj", "--force", "--volumes", "--non-interactive", "--nologo"]);
    return Task.CompletedTask;
  }
}

public class RefusalLines_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RefusalLines_Given_>();

  public static Task NoYes_Should_DescribeTheStopWithoutActing()
  {
    string text = string.Join('\n', DbNuke.BuildRefusalLines("/repo/app-host.csproj"));

    text.ShouldStartWith("Refusing to nuke without --yes");
    text.ShouldContain("stops the AppHost and deletes its Aspire-owned volumes");
    text.ShouldContain("aspire stop --apphost /repo/app-host.csproj --force --volumes");
    text.ShouldContain("dev db reset --yes");
    return Task.CompletedTask;
  }
}

public class AdoptedVolumeHint_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AdoptedVolumeHint_Given_>();

  public static Task RuntimeUnset_Should_NameAspireDefaultCli()
  {
    string text = string.Join('\n', DbNuke.BuildAdoptedVolumeHintLines(null));

    text.ShouldContain("before Aspire 13.6");
    text.ShouldContain("`docker volume ls`, then `docker volume rm <name>`");
    return Task.CompletedTask;
  }

  public static Task RuntimeSet_Should_NameThatCli()
  {
    string text = string.Join('\n', DbNuke.BuildAdoptedVolumeHintLines("podman"));

    text.ShouldContain("`podman volume ls`, then `podman volume rm <name>`");
    text.ShouldNotContain("docker");
    return Task.CompletedTask;
  }

  public static Task RuntimeBlank_Should_FallBackToDefault()
  {
    DbNuke.ContainerRuntimeCli("  ").ShouldBe(DbNuke.DefaultContainerRuntime);
    return Task.CompletedTask;
  }
}

public class CliVersionGuard_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CliVersionGuard_Given_>();

  public static Task Cli136OrLater_Should_AllowNuke()
  {
    AspireRun.ValidateCliVersion("13.6.0+56f3e9c0", DbNuke.MinimumCliVersion, DbNuke.Requirement).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task CliOlderThan136_Should_RefuseWithUpdateCommand()
  {
    string? error = AspireRun.ValidateCliVersion("13.5.4+deadbeef", DbNuke.MinimumCliVersion, DbNuke.Requirement);
    error.ShouldNotBeNull();
    error.ShouldStartWith("`dev db nuke` (aspire stop --force --volumes) requires Aspire CLI 13.6 or later");
    error.ShouldContain("installed: 13.5.4");
    error.ShouldContain("aspire update --self");
    return Task.CompletedTask;
  }
}
