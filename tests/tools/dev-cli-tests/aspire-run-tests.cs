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
    AspireRun.ValidateCliVersion("13.6.0+56f3e9c0", AspireRun.MinimumLaunchProfileVersion, "--launch-profile").ShouldBeNull();
    AspireRun.ValidateCliVersion("14.0.0", AspireRun.MinimumLaunchProfileVersion, "--launch-profile").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task CliOlderThan136_Should_FailWithUpdateCommand()
  {
    string? error = AspireRun.ValidateCliVersion("13.5.4+deadbeef", AspireRun.MinimumLaunchProfileVersion, "--launch-profile");
    error.ShouldNotBeNull();
    error.ShouldStartWith("--launch-profile requires Aspire CLI 13.6 or later");
    error.ShouldContain("13.5.4");
    error.ShouldContain("aspire update --self");
    error.ShouldContain("dotnet tool update -g Aspire.Cli");
    return Task.CompletedTask;
  }

  public static Task UnparseableVersion_Should_FailWithUpdateCommand()
  {
    string? error = AspireRun.ValidateCliVersion("garbage", AspireRun.MinimumLaunchProfileVersion, "--launch-profile");
    error.ShouldNotBeNull();
    error.ShouldContain("aspire update --self");
    return Task.CompletedTask;
  }
}

public class AssessCliVersion_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AssessCliVersion_Given_>();

  private static readonly Version RepoVersion = new(13, 6, 2);

  public static Task OlderThanRepoButAboveMinimum_Should_WarnWithUpdateCommand()
  {
    AspireRun.CliVersionAssessment assessment = AspireRun.AssessCliVersion(
      "13.6.1+56f3e9c0", AspireRun.MinimumLaunchProfileVersion, "--launch-profile", RepoVersion);
    assessment.Error.ShouldBeNull();
    assessment.Warning.ShouldNotBeNull();
    assessment.Warning.ShouldContain("13.6.1");
    assessment.Warning.ShouldContain("13.6.2");
    assessment.Warning.ShouldContain(AspireRun.ToolUpdateCommand);
    return Task.CompletedTask;
  }

  public static Task SameOrNewerThanRepo_Should_NotWarn()
  {
    AspireRun.AssessCliVersion("13.6.2+abc", AspireRun.MinimumLaunchProfileVersion, "--launch-profile", RepoVersion)
      .Warning.ShouldBeNull();
    AspireRun.AssessCliVersion("13.7.0", AspireRun.MinimumLaunchProfileVersion, "--launch-profile", RepoVersion)
      .Warning.ShouldBeNull();
    AspireRun.AssessCliVersion("14.0.0", AspireRun.MinimumLaunchProfileVersion, "--launch-profile", RepoVersion)
      .Error.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task BelowHardMinimum_Should_Refuse()
  {
    AspireRun.CliVersionAssessment assessment = AspireRun.AssessCliVersion(
      "13.5.4+deadbeef", AspireRun.MinimumLaunchProfileVersion, "--launch-profile", RepoVersion);
    assessment.Error.ShouldNotBeNull();
    assessment.Error.ShouldStartWith("--launch-profile requires Aspire CLI 13.6 or later");
    assessment.Error.ShouldContain(AspireRun.ToolUpdateCommand);
    assessment.Warning.ShouldNotBeNull();
    assessment.Warning.ShouldContain(AspireRun.ToolUpdateCommand);
    return Task.CompletedTask;
  }

  public static Task UnparseableVersion_Should_RefuseWithoutALagWarning()
  {
    AspireRun.CliVersionAssessment assessment = AspireRun.AssessCliVersion(
      "garbage", AspireRun.MinimumLaunchProfileVersion, "--launch-profile", RepoVersion);
    assessment.Error.ShouldNotBeNull();
    assessment.Warning.ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class RepoAspireVersion_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RepoAspireVersion_Given_>();

  public static Task PropertyElement_Should_ParseVersion()
  {
    const string props = """
      <Project>
        <PropertyGroup>
          <AspireVersion> 13.6.2 </AspireVersion>
        </PropertyGroup>
      </Project>
      """;
    AspireRun.ParseRepoAspireVersion(props).ShouldBe(new Version(13, 6, 2));
    return Task.CompletedTask;
  }

  public static Task MissingProperty_Should_ReturnNull()
  {
    AspireRun.ParseRepoAspireVersion("<Project></Project>").ShouldBeNull();
    AspireRun.ParseRepoAspireVersion("<AspireVersion>not-a-version</AspireVersion>").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task RepoFiles_Should_ShareOneAspireVersion()
  {
    string root = RepoRoot();
    string? error = AspireRun.ValidateAspireVersionSources(
      File.ReadAllText(Path.Combine(root, AspireRun.DirectoryPackagesPropsFile)),
      File.ReadAllText(Path.Combine(root, AspireRun.AppHostProject)),
      File.ReadAllText(Path.Combine(root, ".github/workflows/workflow.yml")));
    error.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task LiteralPackagePin_Should_Fail()
  {
    const string props = """
      <Project>
        <PropertyGroup><AspireVersion>13.6.2</AspireVersion></PropertyGroup>
        <PackageVersion Include="Aspire.Hosting.Yarp" Version="13.6.2" />
      </Project>
      """;
    string? error = AspireRun.ValidateAspireVersionSources(props, AppHostUsingProperty(), WorkflowReadingProperty());
    error.ShouldNotBeNull();
    error.ShouldContain("$(AspireVersion)");
    return Task.CompletedTask;
  }

  public static Task LiteralSdk_Should_Fail()
  {
    const string props = "<AspireVersion>13.6.2</AspireVersion>";
    const string project = "<Project Sdk=\"Aspire.AppHost.Sdk/13.6.2\">";
    string? error = AspireRun.ValidateAspireVersionSources(props, project, WorkflowReadingProperty());
    error.ShouldNotBeNull();
    error.ShouldContain("literal");
    return Task.CompletedTask;
  }

  public static Task LiteralCliInstall_Should_Fail()
  {
    const string props = "<AspireVersion>13.6.2</AspireVersion>";
    const string workflow = "dotnet tool install --global Aspire.Cli --version 13.6.2";
    string? error = AspireRun.ValidateAspireVersionSources(props, AppHostUsingProperty(), workflow);
    error.ShouldNotBeNull();
    error.ShouldContain("literal");
    return Task.CompletedTask;
  }

  private static string RepoRoot()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, AspireRun.DirectoryPackagesPropsFile)))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }

  private static string AppHostUsingProperty() =>
    "<Project Sdk=\"Microsoft.NET.Sdk\"><Sdk Name=\"Aspire.AppHost.Sdk\" Version=\"$(AspireVersion)\" /></Project>";

  private static string WorkflowReadingProperty() =>
    "version=$(sed -n 's/.*<AspireVersion>\\([^<]*\\)<\\/AspireVersion>.*/\\1/p' Directory.Packages.props)\n"
    + "dotnet tool install --global Aspire.Cli --version \"$version\"";
}
