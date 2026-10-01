#region Purpose
// Gates `dev db nuke` argument building, AppHost volume resolution, the --yes refusal text and the
// shared Aspire CLI version guard, without running Aspire or Docker.
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

  public static Task VolumeList_Should_FilterByPrefix()
  {
    DbNuke.BuildVolumeListArguments("aspire-app-host-0123456789-")
      .ShouldBe(["volume", "ls", "--quiet", "--filter", "name=aspire-app-host-0123456789-"]);
    return Task.CompletedTask;
  }

  public static Task VolumeRemove_Should_NameEachVolume()
  {
    DbNuke.BuildVolumeRemoveArguments(["a-postgres-data", "a-other-data"])
      .ShouldBe(["volume", "rm", "a-postgres-data", "a-other-data"]);
    return Task.CompletedTask;
  }
}

public class VolumeNamePrefix_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<VolumeNamePrefix_Given_>();

  // Shape of Aspire.Hosting 13.6 VolumeNameGenerator: application name (= .csproj file name) plus
  // the first 10 hex of SHA256(lower-cased full .csproj path). Parity with Aspire's REAL generated
  // name is proven in aspire-tests (PostgresVolumeModel_Given_), which compile-includes db-nuke.cs.
  public static Task AppHostPath_Should_MatchAspireVolumeNameGenerator()
  {
    const string path = "/work/app/source/aspire-app-host/aspire-app-host.csproj";
    string expectedHash = Convert.ToHexString(
      System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(path)))[..10].ToLowerInvariant();

    DbNuke.VolumeNamePrefix(path).ShouldBe($"aspire-app-host-{expectedHash}-");
    return Task.CompletedTask;
  }

  public static Task PathCase_Should_NotChangeThePrefix()
  {
    DbNuke.VolumeNamePrefix("/Work/App/aspire-app-host.csproj")
      .ShouldBe(DbNuke.VolumeNamePrefix("/work/app/aspire-app-host.csproj"));
    return Task.CompletedTask;
  }

  public static Task DifferentCheckout_Should_GetADifferentPrefix()
  {
    DbNuke.VolumeNamePrefix("/worktrees/a/aspire-app-host.csproj")
      .ShouldNotBe(DbNuke.VolumeNamePrefix("/worktrees/b/aspire-app-host.csproj"));
    return Task.CompletedTask;
  }

  public static Task InvalidNameChars_Should_BeSanitizedAndLowerCased()
  {
    DbNuke.VolumeNamePrefix("/work/My App+Host.csproj").ShouldStartWith("my_app_host-");
    return Task.CompletedTask;
  }
}

public class FilterAppHostVolumes_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FilterAppHostVolumes_Given_>();

  public static Task SubstringMatches_Should_KeepOnlyPrefixedNamesSorted()
  {
    const string output = """
      aspire-app-host-0123456789-postgres-data
      other-aspire-app-host-0123456789-postgres-data
      aspire-app-host-9999999999-postgres-data

      aspire-app-host-0123456789-cache-data
      """;

    DbNuke.FilterAppHostVolumes(output, "aspire-app-host-0123456789-")
      .ShouldBe(["aspire-app-host-0123456789-cache-data", "aspire-app-host-0123456789-postgres-data"]);
    return Task.CompletedTask;
  }

  public static Task EmptyOutput_Should_ReturnNoVolumes()
  {
    DbNuke.FilterAppHostVolumes("", "aspire-app-host-0123456789-").ShouldBeEmpty();
    return Task.CompletedTask;
  }
}

public class RefusalLines_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RefusalLines_Given_>();

  public static Task Volumes_Should_ListEveryNameAndTheAppHostStop()
  {
    string text = string.Join('\n', DbNuke.BuildRefusalLines(
      "/repo/app-host.csproj", ["a-postgres-data", "a-cache-data"]));

    text.ShouldContain("--yes");
    text.ShouldContain("Stop the running AppHost (/repo/app-host.csproj)");
    text.ShouldContain("2 Docker volume(s)");
    text.ShouldContain("a-postgres-data");
    text.ShouldContain("a-cache-data");
    text.ShouldContain("dev db reset --yes");
    return Task.CompletedTask;
  }

  public static Task NoVolumes_Should_SayNoneExist()
  {
    string text = string.Join('\n', DbNuke.BuildRefusalLines("/repo/app-host.csproj", []));

    text.ShouldContain("None exist right now");
    text.ShouldContain("Stop the running AppHost");
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
