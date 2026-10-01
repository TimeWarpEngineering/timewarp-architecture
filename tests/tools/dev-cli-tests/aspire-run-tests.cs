// ReSharper disable InconsistentNaming
namespace AspireRun_;

public class BuildRunArguments_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BuildRunArguments_Given_>();

  public static Task NoLaunchProfile_Should_BeUnchangedRunApphostArguments()
  {
    AspireRun.BuildRunArguments("/repo/app-host.csproj", null)
      .ShouldBe(["run", "--apphost", "/repo/app-host.csproj"]);
    return Task.CompletedTask;
  }

  public static Task LaunchProfile_Should_AppendLaunchProfileOption()
  {
    AspireRun.BuildRunArguments("/repo/app-host.csproj", "http")
      .ShouldBe(["run", "--apphost", "/repo/app-host.csproj", "--launch-profile", "http"]);
    return Task.CompletedTask;
  }
}

public class ReadLaunchProfileNames_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ReadLaunchProfileNames_Given_>();

  public static Task ProfilesObject_Should_ReturnNamesInDeclarationOrder()
  {
    const string json = """
      {
        // comments and trailing commas are tolerated, as in launchSettings.json
        "profiles": {
          "https": { "commandName": "Project" },
          "http": { "commandName": "Project" },
        }
      }
      """;
    AspireRun.ReadLaunchProfileNames(json).ShouldBe(["https", "http"]);
    return Task.CompletedTask;
  }

  public static Task NoProfiles_Should_ReturnEmpty()
  {
    AspireRun.ReadLaunchProfileNames("{}").ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task RepoAppHostLaunchSettings_Should_DeclareHttpsAndHttp()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, AspireRun.LaunchSettingsFile)))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    string json = File.ReadAllText(Path.Combine(directory, AspireRun.LaunchSettingsFile));
    AspireRun.ReadLaunchProfileNames(json).ShouldBe(["https", "http"]);
    return Task.CompletedTask;
  }
}

public class ValidateLaunchProfile_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ValidateLaunchProfile_Given_>();

  public static Task DeclaredProfile_Should_BeValid()
  {
    AspireRun.ValidateLaunchProfile("http", ["https", "http"]).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task UnknownProfile_Should_ListValidNames()
  {
    string? error = AspireRun.ValidateLaunchProfile("nope", ["https", "http"]);
    error.ShouldNotBeNull();
    error.ShouldContain("'nope'");
    error.ShouldContain("https, http");
    return Task.CompletedTask;
  }

  public static Task DifferentCase_Should_BeRejected()
  {
    AspireRun.ValidateLaunchProfile("HTTP", ["https", "http"]).ShouldNotBeNull();
    return Task.CompletedTask;
  }
}

public class CliVersionGuard_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CliVersionGuard_Given_>();

  public static Task VersionOutput_Should_ParseCoreVersion()
  {
    AspireRun.ParseCliVersion("13.6.0+56f3e9c0d216c0c7069dabb49dd0464e4827744f\n")
      .ShouldBe(new Version(13, 6, 0));
    AspireRun.ParseCliVersion("13.7.0-preview.1.25500.1+abc").ShouldBe(new Version(13, 7, 0));
    AspireRun.ParseCliVersion("not a version").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Cli136OrLater_Should_AllowLaunchProfile()
  {
    AspireRun.ValidateCliVersionForLaunchProfile("13.6.0+56f3e9c0").ShouldBeNull();
    AspireRun.ValidateCliVersionForLaunchProfile("14.0.0").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task CliOlderThan136_Should_FailWithUpdateCommand()
  {
    string? error = AspireRun.ValidateCliVersionForLaunchProfile("13.5.4+deadbeef");
    error.ShouldNotBeNull();
    error.ShouldContain("13.5.4");
    error.ShouldContain("aspire update --self");
    error.ShouldContain("dotnet tool update -g Aspire.Cli");
    return Task.CompletedTask;
  }

  public static Task UnparseableVersion_Should_FailWithUpdateCommand()
  {
    string? error = AspireRun.ValidateCliVersionForLaunchProfile("garbage");
    error.ShouldNotBeNull();
    error.ShouldContain("aspire update --self");
    return Task.CompletedTask;
  }
}
